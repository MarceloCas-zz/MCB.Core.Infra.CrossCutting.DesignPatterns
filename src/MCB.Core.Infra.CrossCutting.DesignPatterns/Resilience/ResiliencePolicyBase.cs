using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Resilience;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Resilience.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using CircuitState = MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Resilience.Enums.CircuitState;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Resilience
{
    public abstract class ResiliencePolicyBase
        : IResiliencePolicy
    {
        // Constants
        private const int EXCEPTIONS_ALLOWED_BEFORE_BREAKING = 1;
        private const string ON_RETRY_LOG_MESSAGE = "ResiliencePolicy|Name:{Name}|Retry|CurrentRetryCount:{CurrentRetryCount}";
        private const string ON_OPEN_LOG_MESSAGE = "ResiliencePolicy|Name:{Name}|CircuitOpen|CurrentCircuitBreakerOpenCount:{CurrentCircuitBreakerOpenCount}";
        private const string ON_CLOSE_LOG_MESSAGE = "ResiliencePolicy|Name:{Name}|CircuitClose";
        private const string ON_CLOSE_MANUALLY_LOG_MESSAGE = "ResiliencePolicy|Name:{Name}|CircuitCloseManually";
        private const string ON_HALF_OPEN_LOG_MESSAGE = "ResiliencePolicy|Name:{Name}|CircuitHalfOpen";
        private const string ON_OPEN_MANUALLY_LOG_MESSAGE = "ResiliencePolicy|Name:{Name}|CircuitOpenManually";
        private const string RETRY_POLICY_CONTEXT_INPUT_KEY = "input";
        private const string RETRY_POLICY_CONTEXT_OUTPUT_KEY = "output";

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
        #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
                        Logger.LogWarning(ON_RETRY_LOG_MESSAGE, ResilienceConfig.Name, CurrentRetryCount);

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
                exceptionsAllowedBeforeBreaking: EXCEPTIONS_ALLOWED_BEFORE_BREAKING,
                durationOfBreak: resilienceConfig.CircuitBreakerWaitingTimeFunction(),
                onBreak: (exception, waitingTime) =>
                {
                    if (resilienceConfig.IsLoggingEnable)
                        Logger.LogWarning(ON_OPEN_LOG_MESSAGE, ResilienceConfig.Name, CurrentCircuitBreakerOpenCount);

                    IncrementCircuitBreakerOpenCount();

                    resilienceConfig.OnCircuitBreakerOpenAditionalHandler?.Invoke((CurrentCircuitBreakerOpenCount, waitingTime, exception));
                },
                onReset: () =>
                {
                    if (resilienceConfig.IsLoggingEnable)
                        Logger.LogWarning(ON_CLOSE_LOG_MESSAGE, ResilienceConfig.Name);

                    ResetCurrentRetryCount();
                    ResetCurrentCircuitBreakerOpenCount();

                    resilienceConfig.OnCircuitBreakerCloseAditionalHandler?.Invoke();
                },
                onHalfOpen: () =>
                {
                    if (resilienceConfig.IsLoggingEnable)
                        Logger.LogWarning(ON_HALF_OPEN_LOG_MESSAGE, ResilienceConfig.Name);

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
                Logger.LogWarning(ON_CLOSE_MANUALLY_LOG_MESSAGE, ResilienceConfig.Name);

            _asyncCircuitBreakerPolicy.Reset();
        }
        public void OpenCircuitBreakerManually()
        {
            if(ResilienceConfig.IsLoggingEnable)
                Logger.LogWarning(ON_OPEN_MANUALLY_LOG_MESSAGE, ResilienceConfig.Name);

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
                            (TInput)context[RETRY_POLICY_CONTEXT_INPUT_KEY]
                        ).ConfigureAwait(false)
                    ).ConfigureAwait(false);
                },
                contextData: new Dictionary<string, object> { { RETRY_POLICY_CONTEXT_INPUT_KEY, input } }
            ).ConfigureAwait(false);

            if (policyResult.Outcome != OutcomeType.Successful)
                return false;

            ResetCurrentRetryCount();
            return true;
        }
        public async Task<(bool success, TOutput output)> ExecuteAsync<TInput, TOutput>(Func<TInput, Task<TOutput>> handler, TInput input)
        {
            bool success;
            var contextData = new Dictionary<string, object> { { RETRY_POLICY_CONTEXT_INPUT_KEY, input } };

            var policyResult = await _asyncCircuitBreakerPolicy.ExecuteAndCaptureAsync(
                async (context) =>
                {
                    context.Add(
                        RETRY_POLICY_CONTEXT_OUTPUT_KEY,
                        await _asyncRetryPolicy.ExecuteAsync(async () =>
                            await handler(
                                (TInput)context[RETRY_POLICY_CONTEXT_INPUT_KEY]
                            ).ConfigureAwait(false)
                        ).ConfigureAwait(false)
                    );
                },
                contextData
            ).ConfigureAwait(false);

            success = policyResult.Outcome == OutcomeType.Successful;

            if (success)
                ResetCurrentRetryCount();

            return (success, output:(TOutput)contextData[RETRY_POLICY_CONTEXT_OUTPUT_KEY]);
        }
    }
}
