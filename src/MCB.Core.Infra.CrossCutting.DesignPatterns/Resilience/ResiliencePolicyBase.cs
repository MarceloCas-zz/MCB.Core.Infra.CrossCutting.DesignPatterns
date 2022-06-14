using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Resilience;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Resilience.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using CircuitState = MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Resilience.Enums.CircuitState;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Resilience;

public abstract class ResiliencePolicyBase
    : IResiliencePolicy
{
    // Static Fields
    private static readonly int exceptionsAllowedBeforeBreaking = 1;
    private static readonly string onRetryLogMessage = "ResiliencePolicy|Name:{Name}|Retry|CurrentRetryCount:{CurrentRetryCount}";
    private static readonly string onOpenLogMessage = "ResiliencePolicy|Name:{Name}|CircuitOpen|CurrentCircuitBreakerOpenCount:{CurrentCircuitBreakerOpenCount}";
    private static readonly string onCloseLogMessage = "ResiliencePolicy|Name:{Name}|CircuitClose";
    private static readonly string onCloseManuallyLogMessage = "ResiliencePolicy|Name:{Name}|CircuitCloseManually";
    private static readonly string onHalfOpenLogMessage = "ResiliencePolicy|Name:{Name}|CircuitHalfOpen";
    private static readonly string onOpenManuallyLogMessage = "ResiliencePolicy|Name:{Name}|CircuitOpenManually";
    private static readonly string retryPolicyContextInputKey = "input";
    private static readonly string retryPolicyContextOutputKey = "output";

    // Fields
    private AsyncRetryPolicy _asyncRetryPolicy;
    private AsyncCircuitBreakerPolicy _asyncCircuitBreakerPolicy;

    // Protected Properties
    protected ILogger Logger { get; }

    // Public Properties
    public string Name { get; private set; }

    public CircuitState CircuitState => GetCircuitState(_asyncCircuitBreakerPolicy.CircuitState);

    public int CurrentRetryCount {get; private set; }
    public int CurrentCircuitBreakerOpenCount { get; private set; }
    public ResilienceConfig ResilienceConfig { get; private set; }

    // Constructors
    protected ResiliencePolicyBase(ILogger logger)
    {
        Logger = logger;
        ResilienceConfig = new ResilienceConfig();

        ApplyConfig(ResilienceConfig);
        ResetCurrentCircuitBreakerOpenCount();
    }

    // Private Methods

    private void ResetCurrentRetryCount() => CurrentRetryCount = 0;
    private void IncrementRetryCount() => CurrentRetryCount++;
    private void ResetCurrentCircuitBreakerOpenCount() => CurrentCircuitBreakerOpenCount = 0;
    private void IncrementCircuitBreakerOpenCount() => CurrentCircuitBreakerOpenCount++;
    private void ConfigureRetryPolicy(ResilienceConfig resilienceConfig)
    {
        var retryPolicyBuilder = default(PolicyBuilder);
        foreach (var exceptionHandleConfig in resilienceConfig.ExceptionHandleConfigArray)
        {
            if (retryPolicyBuilder is null)
                retryPolicyBuilder = Policy.Handle(exceptionHandleConfig);
            else
                retryPolicyBuilder = retryPolicyBuilder.Or(exceptionHandleConfig);
        }

        _asyncRetryPolicy = retryPolicyBuilder.WaitAndRetryAsync(
            retryCount: resilienceConfig.RetryMaxAttemptCount,
            sleepDurationProvider: resilienceConfig.RetryAttemptWaitingTimeFunction,
            onRetry: (exception, retryAttemptWaitingTime) =>
            {
                IncrementRetryCount();

                if (resilienceConfig.IsLoggingEnable)
                    Logger.LogWarning(onRetryLogMessage, ResilienceConfig.Name, CurrentRetryCount);

                resilienceConfig.OnRetryAditionalHandler?.Invoke((CurrentRetryCount, retryAttemptWaitingTime, exception));
            }
        );
    }
    private void ConfigureCircuitBreakerPolicy(ResilienceConfig resilienceConfig)
    {
        var circuitBreakerPolicyBuilder = default(PolicyBuilder);

        foreach (var exceptionHandleConfig in resilienceConfig.ExceptionHandleConfigArray)
        {
            if (circuitBreakerPolicyBuilder is null)
                circuitBreakerPolicyBuilder = Policy.Handle(exceptionHandleConfig);
            else
                circuitBreakerPolicyBuilder = circuitBreakerPolicyBuilder.Or(exceptionHandleConfig);
        }

        _asyncCircuitBreakerPolicy = circuitBreakerPolicyBuilder.CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: exceptionsAllowedBeforeBreaking,
            durationOfBreak: resilienceConfig.CircuitBreakerWaitingTimeFunction(),
            onBreak: (exception, waitingTime) =>
            {
                if (resilienceConfig.IsLoggingEnable)
                    Logger.LogWarning(onOpenLogMessage, ResilienceConfig.Name, CurrentCircuitBreakerOpenCount);

                IncrementCircuitBreakerOpenCount();

                resilienceConfig.OnCircuitBreakerOpenAditionalHandler?.Invoke((CurrentCircuitBreakerOpenCount, waitingTime, exception));
            },
            onReset: () =>
            {
                if (resilienceConfig.IsLoggingEnable)
                    Logger.LogWarning(onCloseLogMessage, ResilienceConfig.Name);

                ResetCurrentRetryCount();
                ResetCurrentCircuitBreakerOpenCount();

                resilienceConfig.OnCircuitBreakerCloseAditionalHandler?.Invoke();
            },
            onHalfOpen: () =>
            {
                if (resilienceConfig.IsLoggingEnable)
                    Logger.LogWarning(onHalfOpenLogMessage, ResilienceConfig.Name);

                ResetCurrentRetryCount();

                resilienceConfig.OnCircuitBreakerHalfOpenAditionalHandler?.Invoke();
            }
        );
    }
    private void ApplyConfig(ResilienceConfig resilienceConfig)
    {
        Name = resilienceConfig.Name;

        // Retry
        ConfigureRetryPolicy(resilienceConfig);

        // Circuit Breaker
        ConfigureCircuitBreakerPolicy(resilienceConfig);
    }

    // Protected Methods
    protected static CircuitState GetCircuitState(Polly.CircuitBreaker.CircuitState pollyCircuitState) =>
        pollyCircuitState switch
        {
            Polly.CircuitBreaker.CircuitState.Closed => CircuitState.Closed,
            Polly.CircuitBreaker.CircuitState.Open => CircuitState.Open,
            Polly.CircuitBreaker.CircuitState.HalfOpen => CircuitState.HalfOpen,
            Polly.CircuitBreaker.CircuitState.Isolated => CircuitState.Isolated,
            _ => throw new ArgumentOutOfRangeException(nameof(pollyCircuitState))
        };

    // Public Methods
    public void Configure(Action<ResilienceConfig> configureAction)
    {
        var resilienceConfig = new ResilienceConfig();

        configureAction(resilienceConfig);

        ResilienceConfig = resilienceConfig;
        ApplyConfig(ResilienceConfig);
    }
    public void CloseCircuitBreakerManually()
    {
        if (ResilienceConfig.IsLoggingEnable)
            Logger.LogWarning(onCloseManuallyLogMessage, ResilienceConfig.Name);

        _asyncCircuitBreakerPolicy.Reset();
    }
    public void OpenCircuitBreakerManually()
    {
        if(ResilienceConfig.IsLoggingEnable)
            Logger.LogWarning(onOpenManuallyLogMessage, ResilienceConfig.Name);

        _asyncCircuitBreakerPolicy.Isolate();
    }

    public async Task<bool> ExecuteAsync(Func<Task> handler)
    {
        var policyResult = await _asyncCircuitBreakerPolicy.ExecuteAndCaptureAsync(
            async () =>
            {
                await _asyncRetryPolicy.ExecuteAsync(async () =>
                    await handler().ConfigureAwait(false)
                ).ConfigureAwait(false);
            }
        ).ConfigureAwait(false);

        if (policyResult.Outcome != OutcomeType.Successful)
            return false;

        ResetCurrentRetryCount();
        return true;
    }
    public async Task<bool> ExecuteAsync<TInput>(Func<TInput, Task> handler, TInput input)
    {
        var policyResult = await _asyncCircuitBreakerPolicy.ExecuteAndCaptureAsync(
            async (context) =>
            {
                await _asyncRetryPolicy.ExecuteAsync(async () =>
                    await handler(
                        (TInput)context[retryPolicyContextInputKey]
                    ).ConfigureAwait(false)
                ).ConfigureAwait(false);
            },
            contextData: new Dictionary<string, object> { { retryPolicyContextInputKey, input } }
        ).ConfigureAwait(false);

        if (policyResult.Outcome != OutcomeType.Successful)
            return false;

        ResetCurrentRetryCount();
        return true;
    }
    public async Task<(bool success, TOutput output)> ExecuteAsync<TInput, TOutput>(Func<TInput, Task<TOutput>> handler, TInput input)
    {
        var policyResult = await _asyncCircuitBreakerPolicy.ExecuteAndCaptureAsync(
            async (context) =>
            {
                context.Add(
                    retryPolicyContextOutputKey,
                    await _asyncRetryPolicy.ExecuteAsync(async () =>
                        await handler(
                            (TInput)context[retryPolicyContextInputKey]
                        ).ConfigureAwait(false)
                    ).ConfigureAwait(false)
                );
            },
            contextData: new Dictionary<string, object> { { retryPolicyContextInputKey, input } }
        ).ConfigureAwait(false);

        var success = policyResult.Outcome == OutcomeType.Successful;

        if (!success)
            return (success: false, output: default);

        ResetCurrentRetryCount();

        return (success, output: (TOutput)policyResult.Context[retryPolicyContextOutputKey]);
    }
}
