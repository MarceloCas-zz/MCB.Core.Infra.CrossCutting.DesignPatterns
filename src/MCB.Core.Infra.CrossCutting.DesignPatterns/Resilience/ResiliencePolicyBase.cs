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
        private const string ASYNC_RETRY_POLICY_CANNOT_BE_NULL = "AsyncRetryPolicy cannot be null";
        private const string ASYNC_CIRCUIT_BREAKER_POLICY_CANNOT_BE_NULL = "AsyncCircuitBreakerPolicy cannot be null";
        private const string ON_RETRY_LOG_MESSAGE = "ResiliencePolicy|Name:{0}|Retry|CurrentRetryCount:{1}";
        private const string ON_OPEN_LOG_MESSAGE = "ResiliencePolicy|Name:{0}|CircuitOpen|CurrentCircuitBreakerOpenCount:{1}";
        private const string ON_CLOSE_MANUALLY_LOG_MESSAGE = "ResiliencePolicy|Name:{0}|CircuitCloseManually";
        private const string ON_HALF_OPEN_LOG_MESSAGE = "ResiliencePolicy|Name:{0}|CircuitHalfOpen";
        private const string ON_OPEN_MANUALLY_LOG_MESSAGE = "ResiliencePolicy|Name:{0}|CircuitOpenManually";

        // Fields
        private AsyncRetryPolicy? _asyncRetryPolicy;
        private AsyncCircuitBreakerPolicy? _asyncCircuitBreakerPolicy;

        // Protected Properties
        protected ILogger Logger { get; }

        // Public Properties
        public string Name { get; private set; }
        public CircuitState CircuitState => _asyncCircuitBreakerPolicy?.CircuitState switch
        {
            Polly.CircuitBreaker.CircuitState.Closed => CircuitState.Closed,
            Polly.CircuitBreaker.CircuitState.Open => CircuitState.Open,
            Polly.CircuitBreaker.CircuitState.HalfOpen => CircuitState.HalfOpen,
            Polly.CircuitBreaker.CircuitState.Isolated => CircuitState.Isolated,
            _ => 0,
        };
        public int CurrentRetryCount {get; private set; }
        public int CurrentCircuitBreakerOpenCount { get; private set; }
        public ResilienceConfig ResilienceConfig { get; private set; }

        // Constructors
        protected ResiliencePolicyBase(ILogger logger)
        {
            Logger = logger;
            ResilienceConfig = new ResilienceConfig();
            Name = this.GetType().Name;

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
                        Logger.LogWarning(ON_CLOSE_MANUALLY_LOG_MESSAGE, ResilienceConfig.Name);

                    ResetCurrentRetryCount();
                    ResetCurrentCircuitBreakerOpenCount();

                    resilienceConfig.OnCircuitBreakerResetOpenAditionalHandler?.Invoke();
                },
                onHalfOpen: () =>
                {
                    if (resilienceConfig.IsLoggingEnable)
                        Logger.LogWarning(ON_HALF_OPEN_LOG_MESSAGE, ResilienceConfig.Name);

                    ResetCurrentRetryCount();
                    ResetCurrentCircuitBreakerOpenCount();

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
        private void ValidatePreExecution()
        {
            if (_asyncRetryPolicy is null)
                throw new ArgumentNullException(paramName: nameof(_asyncRetryPolicy), ASYNC_RETRY_POLICY_CANNOT_BE_NULL);
            if (_asyncCircuitBreakerPolicy is null)
                throw new ArgumentNullException(paramName: nameof(_asyncCircuitBreakerPolicy), ASYNC_CIRCUIT_BREAKER_POLICY_CANNOT_BE_NULL);
        }

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
            // Log is write in onReset handler durring _asyncCircuitBreakerPolicy configuration
            _asyncCircuitBreakerPolicy?.Reset();
        }
        public void OpenCircuitBreakerManually()
        {
            if(ResilienceConfig.IsLoggingEnable)
                Logger.LogWarning(ON_OPEN_MANUALLY_LOG_MESSAGE, ResilienceConfig.Name);

            _asyncCircuitBreakerPolicy?.Isolate();
        }
        public async Task<bool> ExecuteAsync(Func<Task> handler)
        {
            try
            {
                ValidatePreExecution();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var policyResult = await _asyncCircuitBreakerPolicy.ExecuteAndCaptureAsync(async () =>
                    await _asyncRetryPolicy.ExecuteAsync(async () =>
                        await handler().ConfigureAwait(false)
                    ).ConfigureAwait(false)
                ).ConfigureAwait(false);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                if (policyResult.Outcome != OutcomeType.Successful)
                    return false;

                ResetCurrentRetryCount();
                return true;
            }
            catch (Exception ex)
            {
                if(ResilienceConfig.IsLoggingEnable)
                    Logger.LogError(ex.Message);

                throw;
            }
        }
    }
}
