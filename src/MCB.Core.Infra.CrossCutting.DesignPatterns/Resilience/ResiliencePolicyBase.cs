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

        // Fields
        private AsyncRetryPolicy _asyncRetryPolicy;
        private AsyncCircuitBreakerPolicy _asyncCircuitBreakerPolicy;

        // Protected Properties
        protected ILogger Logger { get; }

        // Public Properties
        public string Name { get; private set; }
        public CircuitState CircuitState => _asyncCircuitBreakerPolicy.CircuitState switch
        {
            Polly.CircuitBreaker.CircuitState.Closed => CircuitState.Closed,
            Polly.CircuitBreaker.CircuitState.Open => CircuitState.Open,
            Polly.CircuitBreaker.CircuitState.HalfOpen => CircuitState.HalfOpen,
            Polly.CircuitBreaker.CircuitState.Isolated => CircuitState.Isolated,
            _ => 0,
        };
        public int CircuitBreakerOpenCount { get; private set; }
        public ResilienceConfig ResilienceConfig { get; private set; }

        // Constructors
        protected ResiliencePolicyBase(ILogger logger)
        {
            Logger = logger;
            ResilienceConfig = new ResilienceConfig();

            ApplyConfig(ResilienceConfig);
            ResetCircuitBreakerOpenCount();
        }

        // Private Methods
        private void ResetCircuitBreakerOpenCount() => CircuitBreakerOpenCount = 0;
        private void IncrementCircuitBreakerOpenCount() => CircuitBreakerOpenCount++;
        private void ApplyConfig(ResilienceConfig resilienceConfig)
        {
            Name = resilienceConfig.Name;

            // Retry
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
                onRetry: (exception, retryAttemptWaitingTime) => {
                    Logger.LogError("");
                    resilienceConfig.OnRetryAditionalHandler?.Invoke((exception, retryAttemptWaitingTime));
                }
            );

            // Circuit Breaker
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
                onBreak: (exception, waitingTime) => {
                    Logger.LogError("");
                    resilienceConfig.OnCircuitBreakerOpenAditionalHandler?.Invoke((exception, waitingTime));
                },
                onReset: () => {
                    Logger.LogError("");
                    resilienceConfig.OnCircuitBreakerResetOpenAditionalHandler?.Invoke();
                },
                onHalfOpen: () => {
                    Logger.LogError("");
                    resilienceConfig.OnCircuitBreakerHalfOpenAditionalHandler?.Invoke();
                }
            );
        }

        // Public Methods
        public void Configure(Action<ResilienceConfig> config)
        {
            var resilienceConfig = new ResilienceConfig();

            config(resilienceConfig);

            ResilienceConfig = resilienceConfig;
        }
        public void CloseCircuitBreakerManually()
        {
            Logger.LogWarning("");
            _asyncCircuitBreakerPolicy.Reset();
        }
        public void OpenCircuitBreakerManually()
        {
            Logger.LogWarning("");
            _asyncCircuitBreakerPolicy.Isolate();
            IncrementCircuitBreakerOpenCount();
        }
        public async Task ExecuteAsync(Func<Task> handler)
        {
            try
            {
                await _asyncCircuitBreakerPolicy.ExecuteAndCaptureAsync(async () =>
                    await _asyncRetryPolicy.ExecuteAsync(async () =>
                        await handler().ConfigureAwait(false)
                    ).ConfigureAwait(false)
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                throw;
            }
        }
        
    }
}
