using FluentAssertions;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Resilience.Enums;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Resilience;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Tests.ResilienceTests
{
    [Collection(nameof(DefaultFixture))]
    public class ResiliencePolicyBaseTest
    {
        // Fields
        private readonly DefaultFixture _fixture;

        // Constructors
        public ResiliencePolicyBaseTest(
            DefaultFixture fixture    
        )
        {
            _fixture = fixture;
        }

        // Private Methods
        #pragma warning disable CS8604 // Possible null reference argument.
        private ResiliencePolicyWithAllConfig CreateResiliencePolicyWithAllConfig() => new(
            _fixture.ServiceProvider.GetService<ILogger<ResiliencePolicyWithAllConfig>>()
        );
        #pragma warning restore CS8604 // Possible null reference argument.

        #pragma warning disable CS8604 // Possible null reference argument.
        private ResiliencePolicyWithMinimumConfig CreateResiliencePolicyWithMinimumConfig() => new(
            _fixture.ServiceProvider.GetService<ILogger<ResiliencePolicyWithMinimumConfig>>()
        );
        #pragma warning restore CS8604 // Possible null reference argument.

        [Fact]
        public void ResiliencePolicy_Should_Get_Correctly_Status()
        {
            // Arrange
            var pollyClosedStatus = Polly.CircuitBreaker.CircuitState.Closed;
            var pollyOpenStatus = Polly.CircuitBreaker.CircuitState.Open;
            var pollyHalfOpenStatus = Polly.CircuitBreaker.CircuitState.HalfOpen;
            var pollyIsolatedOpenStatus = Polly.CircuitBreaker.CircuitState.Isolated;
            var invalidPollyStatus = (Polly.CircuitBreaker.CircuitState)int.MaxValue;
            var hasErrorOnInvalidPollyStatus = false;

            // Act
            var closedStatus = ResiliencePolicyWithAllConfig.GetCircuitState(pollyClosedStatus);
            var openStatus = ResiliencePolicyWithAllConfig.GetCircuitState(pollyOpenStatus);
            var halfOpenStatus = ResiliencePolicyWithAllConfig.GetCircuitState(pollyHalfOpenStatus);
            var isolatedOpenStatus = ResiliencePolicyWithAllConfig.GetCircuitState(pollyIsolatedOpenStatus);
            try
            {
                ResiliencePolicyWithAllConfig.GetCircuitState(invalidPollyStatus);
            }
            catch (ArgumentOutOfRangeException)
            {
                hasErrorOnInvalidPollyStatus = true;
            }

            // Assert
            closedStatus.Should().Be(CircuitState.Closed);
            openStatus.Should().Be(CircuitState.Open);
            halfOpenStatus.Should().Be(CircuitState.HalfOpen);
            isolatedOpenStatus.Should().Be(CircuitState.Isolated);
            hasErrorOnInvalidPollyStatus.Should().BeTrue();
        }

        [Fact]
        public async Task ResiliencePolicy_Should_Execute_With_Success()
        {
            // Arrange
            var resiliencePolicyWithAllConfig = CreateResiliencePolicyWithAllConfig();
            var resiliencePolicyWithMinimumConfig = CreateResiliencePolicyWithMinimumConfig();
            var successOnRunResiliencePolicyWithAllConfig = false;
            var successOnRunResiliencePolicyWithMinimumConfig = false;

            // Act
            successOnRunResiliencePolicyWithAllConfig = await resiliencePolicyWithAllConfig.ExecuteAsync(() => {
                return Task.CompletedTask;
            });
            successOnRunResiliencePolicyWithMinimumConfig = await resiliencePolicyWithMinimumConfig.ExecuteAsync(() => {
                return Task.CompletedTask;
            });

            // Assert
            successOnRunResiliencePolicyWithAllConfig.Should().BeTrue();
            resiliencePolicyWithAllConfig.CircuitState.Should().Be(CircuitState.Closed);
            resiliencePolicyWithAllConfig.CurrentCircuitBreakerOpenCount.Should().Be(0);
            resiliencePolicyWithAllConfig.CurrentRetryCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerHalfOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerCloseAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnRetryAditionalHandlerCount.Should().Be(0);

            successOnRunResiliencePolicyWithMinimumConfig.Should().BeTrue();
            resiliencePolicyWithMinimumConfig.CircuitState.Should().Be(CircuitState.Closed);
            resiliencePolicyWithMinimumConfig.CurrentCircuitBreakerOpenCount.Should().Be(0);
            resiliencePolicyWithMinimumConfig.CurrentRetryCount.Should().Be(0);
        }

        [Fact]
        public async Task ResiliencePolicy_Should_Fail()
        {
            // Arrange
            var resiliencePolicyWithAllConfig = CreateResiliencePolicyWithAllConfig();
            var resiliencePolicyWithMinimumConfig = CreateResiliencePolicyWithMinimumConfig();
            var successOnRunResiliencePolicyWithAllConfig = false;
            var successOnRunResiliencePolicyWithMinimumConfig = false;

            // Act
            var stopwatch = Stopwatch.StartNew();
            successOnRunResiliencePolicyWithAllConfig = await resiliencePolicyWithAllConfig.ExecuteAsync(() =>
            {
                throw new ArgumentException();
            });

            successOnRunResiliencePolicyWithMinimumConfig = await resiliencePolicyWithMinimumConfig.ExecuteAsync(() =>
            {
                throw new ArgumentException();
            });
            stopwatch.Stop();

            // Assert
            successOnRunResiliencePolicyWithAllConfig.Should().BeFalse();
            resiliencePolicyWithAllConfig.CircuitState.Should().Be(CircuitState.Open);
            resiliencePolicyWithAllConfig.CurrentCircuitBreakerOpenCount.Should().Be(1);
            resiliencePolicyWithAllConfig.CurrentRetryCount.Should().Be(resiliencePolicyWithAllConfig.ResilienceConfig.RetryMaxAttemptCount);
            resiliencePolicyWithAllConfig.OnCircuitBreakerHalfOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerOpenAditionalHandlerCount.Should().Be(1);
            resiliencePolicyWithAllConfig.OnCircuitBreakerCloseAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnRetryAditionalHandlerCount.Should().Be(resiliencePolicyWithAllConfig.ResilienceConfig.RetryMaxAttemptCount);

            resiliencePolicyWithMinimumConfig.CircuitState.Should().Be(CircuitState.Open);
            successOnRunResiliencePolicyWithMinimumConfig.Should().BeFalse();
            resiliencePolicyWithMinimumConfig.CurrentCircuitBreakerOpenCount.Should().Be(1);
            resiliencePolicyWithMinimumConfig.CurrentRetryCount.Should().Be(resiliencePolicyWithMinimumConfig.ResilienceConfig.RetryMaxAttemptCount);
        }

        [Fact]
        public async Task ResiliencePolicy_Should_Not_Execute_Any_Policy_When_Throw_Exception_Not_Handled()
        {
            // Arrange
            var resiliencePolicyWithAllConfig = CreateResiliencePolicyWithAllConfig();
            var successOnRunResiliencePolicyWithAllConfig = false;

            // Act
            successOnRunResiliencePolicyWithAllConfig = await resiliencePolicyWithAllConfig.ExecuteAsync(() =>
            {
                throw new Exception();
            });

            // Assert
            successOnRunResiliencePolicyWithAllConfig.Should().BeFalse();
            resiliencePolicyWithAllConfig.CircuitState.Should().Be(CircuitState.Closed);
            resiliencePolicyWithAllConfig.CurrentCircuitBreakerOpenCount.Should().Be(0);
            resiliencePolicyWithAllConfig.CurrentRetryCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerHalfOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerCloseAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnRetryAditionalHandlerCount.Should().Be(0);
        }

        [Fact]
        public async Task ResiliencePolicy_Should_Apply_Retry_Policy()
        {
            // Arrange
            var resiliencePolicyWithAllConfig = CreateResiliencePolicyWithAllConfig();
            var successOnRunResiliencePolicyWithAllConfig = false;

            // Act
            var stopwatch = Stopwatch.StartNew();
            successOnRunResiliencePolicyWithAllConfig = await resiliencePolicyWithAllConfig.ExecuteAsync(() =>
            {
                if (resiliencePolicyWithAllConfig.CurrentRetryCount < resiliencePolicyWithAllConfig.ResilienceConfig.RetryMaxAttemptCount)
                    throw new InvalidOperationException();

                return Task.CompletedTask;
            });
            stopwatch.Stop();

            // Assert
            successOnRunResiliencePolicyWithAllConfig.Should().BeTrue();
            resiliencePolicyWithAllConfig.CircuitState.Should().Be(CircuitState.Closed);
            resiliencePolicyWithAllConfig.CurrentCircuitBreakerOpenCount.Should().Be(0);
            resiliencePolicyWithAllConfig.CurrentRetryCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerHalfOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerCloseAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnRetryAditionalHandlerCount.Should().Be(resiliencePolicyWithAllConfig.ResilienceConfig.RetryMaxAttemptCount);
        }

        [Fact]
        public async Task ResiliencePolicy_Should_Be_HalfOpen_CircuitStatus()
        {
            // Arrange
            var resiliencePolicyWithAllConfig = CreateResiliencePolicyWithAllConfig();
            var resiliencePolicyWithMinimumConfig = CreateResiliencePolicyWithMinimumConfig();
            var successOnRunResiliencePolicyWithAllConfig = false;
            var successOnRunResiliencePolicyWithMinimumlConfig = false;

            // Act
            successOnRunResiliencePolicyWithAllConfig = await resiliencePolicyWithAllConfig.ExecuteAsync(() =>
            {
                throw new ArgumentException();
            });
            await Task.Delay(resiliencePolicyWithAllConfig.ResilienceConfig.CircuitBreakerWaitingTimeFunction().Add(TimeSpan.FromSeconds(1)));

            successOnRunResiliencePolicyWithMinimumlConfig = await resiliencePolicyWithMinimumConfig.ExecuteAsync(() =>
            {
                throw new ArgumentException();
            });
            await Task.Delay(resiliencePolicyWithMinimumConfig.ResilienceConfig.CircuitBreakerWaitingTimeFunction().Add(TimeSpan.FromSeconds(1)));

            // Assert
            successOnRunResiliencePolicyWithAllConfig.Should().BeFalse();
            resiliencePolicyWithAllConfig.CircuitState.Should().Be(CircuitState.HalfOpen);
            resiliencePolicyWithAllConfig.CurrentCircuitBreakerOpenCount.Should().Be(1);
            resiliencePolicyWithAllConfig.CurrentRetryCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerHalfOpenAditionalHandlerCount.Should().Be(1);
            resiliencePolicyWithAllConfig.OnCircuitBreakerOpenAditionalHandlerCount.Should().Be(1);
            resiliencePolicyWithAllConfig.OnCircuitBreakerCloseAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnRetryAditionalHandlerCount.Should().Be(resiliencePolicyWithAllConfig.ResilienceConfig.RetryMaxAttemptCount);

            successOnRunResiliencePolicyWithMinimumlConfig.Should().BeFalse();
            resiliencePolicyWithMinimumConfig.CircuitState.Should().Be(CircuitState.HalfOpen);
            resiliencePolicyWithMinimumConfig.CurrentCircuitBreakerOpenCount.Should().Be(1);
            resiliencePolicyWithMinimumConfig.CurrentRetryCount.Should().Be(0);
        }

        [Fact]
        public async Task ResiliencePolicy_Should_Be_Closed_After_Immediately_Success_During_HalfOpen_CircuitStatus()
        {
            // Arrange
            var resiliencePolicyWithAllConfig = CreateResiliencePolicyWithAllConfig();
            var resiliencePolicyWithMinimumConfig = CreateResiliencePolicyWithMinimumConfig();
            var successOnRunResiliencePolicyWithAllConfig = false;
            var successOnRunResiliencePolicyWithMinimumConfig = false;

            // Act
            successOnRunResiliencePolicyWithAllConfig = await resiliencePolicyWithAllConfig.ExecuteAsync(() =>
            {
                throw new ArgumentException();
            });
            await Task.Delay(resiliencePolicyWithAllConfig.ResilienceConfig.CircuitBreakerWaitingTimeFunction().Add(TimeSpan.FromSeconds(1)));
            successOnRunResiliencePolicyWithAllConfig = await resiliencePolicyWithAllConfig.ExecuteAsync(() =>
            {
                return Task.CompletedTask;
            });

            successOnRunResiliencePolicyWithMinimumConfig = await resiliencePolicyWithMinimumConfig.ExecuteAsync(() =>
            {
                throw new ArgumentException();
            });
            await Task.Delay(resiliencePolicyWithMinimumConfig.ResilienceConfig.CircuitBreakerWaitingTimeFunction().Add(TimeSpan.FromSeconds(1)));
            successOnRunResiliencePolicyWithMinimumConfig = await resiliencePolicyWithMinimumConfig.ExecuteAsync(() =>
            {
                return Task.CompletedTask;
            });

            // Assert
            successOnRunResiliencePolicyWithAllConfig.Should().BeTrue();
            resiliencePolicyWithAllConfig.CircuitState.Should().Be(CircuitState.Closed);
            resiliencePolicyWithAllConfig.CurrentCircuitBreakerOpenCount.Should().Be(0);
            resiliencePolicyWithAllConfig.CurrentRetryCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerHalfOpenAditionalHandlerCount.Should().Be(1);
            resiliencePolicyWithAllConfig.OnCircuitBreakerOpenAditionalHandlerCount.Should().Be(1);
            resiliencePolicyWithAllConfig.OnCircuitBreakerCloseAditionalHandlerCount.Should().Be(1);
            resiliencePolicyWithAllConfig.OnRetryAditionalHandlerCount.Should().Be(resiliencePolicyWithAllConfig.ResilienceConfig.RetryMaxAttemptCount);

            successOnRunResiliencePolicyWithMinimumConfig.Should().BeTrue();
            resiliencePolicyWithMinimumConfig.CircuitState.Should().Be(CircuitState.Closed);
            resiliencePolicyWithMinimumConfig.CurrentCircuitBreakerOpenCount.Should().Be(0);
            resiliencePolicyWithMinimumConfig.CurrentRetryCount.Should().Be(0);
        }
        [Fact]
        public async Task ResiliencePolicy_Should_Be_Closed_After_Success_During_HalfOpen_CircuitStatus()
        {
            // Arrange
            var resiliencePolicyWithAllConfig = CreateResiliencePolicyWithAllConfig();
            var resiliencePolicyWithMinimumConfig = CreateResiliencePolicyWithMinimumConfig();
            var successOnRunResiliencePolicyWithAllConfig = false;
            var successOnRunResiliencePolicyWithMinimumConfig = false;

            // Act
            successOnRunResiliencePolicyWithAllConfig = await resiliencePolicyWithAllConfig.ExecuteAsync(() =>
            {
                throw new ArgumentException();
            });
            await Task.Delay(resiliencePolicyWithAllConfig.ResilienceConfig.CircuitBreakerWaitingTimeFunction().Add(TimeSpan.FromSeconds(1)));
            successOnRunResiliencePolicyWithAllConfig = await resiliencePolicyWithAllConfig.ExecuteAsync(() =>
            {
                if (resiliencePolicyWithAllConfig.CurrentRetryCount < resiliencePolicyWithAllConfig.ResilienceConfig.RetryMaxAttemptCount)
                    throw new InvalidOperationException();

                return Task.CompletedTask;
            });

            successOnRunResiliencePolicyWithMinimumConfig = await resiliencePolicyWithMinimumConfig.ExecuteAsync(() =>
            {
                throw new ArgumentException();
            });
            await Task.Delay(resiliencePolicyWithMinimumConfig.ResilienceConfig.CircuitBreakerWaitingTimeFunction().Add(TimeSpan.FromSeconds(1)));
            successOnRunResiliencePolicyWithMinimumConfig = await resiliencePolicyWithMinimumConfig.ExecuteAsync(() =>
            {
                if (resiliencePolicyWithMinimumConfig.CurrentRetryCount < resiliencePolicyWithMinimumConfig.ResilienceConfig.RetryMaxAttemptCount)
                    throw new InvalidOperationException();

                return Task.CompletedTask;
            });

            // Assert
            successOnRunResiliencePolicyWithAllConfig.Should().BeTrue();
            resiliencePolicyWithAllConfig.CircuitState.Should().Be(CircuitState.Closed);
            resiliencePolicyWithAllConfig.CurrentCircuitBreakerOpenCount.Should().Be(0);
            resiliencePolicyWithAllConfig.CurrentRetryCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerHalfOpenAditionalHandlerCount.Should().Be(1);
            resiliencePolicyWithAllConfig.OnCircuitBreakerOpenAditionalHandlerCount.Should().Be(1);
            resiliencePolicyWithAllConfig.OnCircuitBreakerCloseAditionalHandlerCount.Should().Be(1);
            resiliencePolicyWithAllConfig.OnRetryAditionalHandlerCount.Should().Be(resiliencePolicyWithAllConfig.ResilienceConfig.RetryMaxAttemptCount * 2);

            successOnRunResiliencePolicyWithMinimumConfig.Should().BeTrue();
            resiliencePolicyWithMinimumConfig.CircuitState.Should().Be(CircuitState.Closed);
            resiliencePolicyWithMinimumConfig.CurrentCircuitBreakerOpenCount.Should().Be(0);
            resiliencePolicyWithMinimumConfig.CurrentRetryCount.Should().Be(0);
        }

        [Fact]
        public async Task ResiliencePolicy_Should_Be_Opened_Manually()
        {
            // Arrange
            var resiliencePolicyWithAllConfig = CreateResiliencePolicyWithAllConfig();
            var resiliencePolicyWithMinimumConfig = CreateResiliencePolicyWithMinimumConfig();
            var successOnRunResiliencePolicyWithAllConfig = await resiliencePolicyWithAllConfig.ExecuteAsync(() => {
                return Task.CompletedTask;
            });
            var successOnRunResiliencePolicyWithMinimumConfig = await resiliencePolicyWithMinimumConfig.ExecuteAsync(() => {
                return Task.CompletedTask;
            });
            var successOnRunResiliencePolicyAfterManuallyOpenedWithAllConfig = false;
            var successOnRunResiliencePolicyAfterManuallyOpenedWithMinimumConfig = false;

            // Act
            resiliencePolicyWithAllConfig.OpenCircuitBreakerManually();
            resiliencePolicyWithMinimumConfig.OpenCircuitBreakerManually();

            successOnRunResiliencePolicyAfterManuallyOpenedWithAllConfig = await resiliencePolicyWithMinimumConfig.ExecuteAsync(() => {
                return Task.CompletedTask;
            });
            successOnRunResiliencePolicyAfterManuallyOpenedWithMinimumConfig = await resiliencePolicyWithMinimumConfig.ExecuteAsync(() => {
                return Task.CompletedTask;
            });

            // Assert
            successOnRunResiliencePolicyWithAllConfig.Should().BeTrue();
            successOnRunResiliencePolicyWithMinimumConfig.Should().BeTrue();
            successOnRunResiliencePolicyAfterManuallyOpenedWithAllConfig.Should().BeFalse();
            successOnRunResiliencePolicyAfterManuallyOpenedWithMinimumConfig.Should().BeFalse();
        }
    }

    public class ResiliencePolicyWithAllConfig
        : ResiliencePolicyBase
    {
        // Properties
        public int OnRetryAditionalHandlerCount { get; private set; }
        public int OnCircuitBreakerHalfOpenAditionalHandlerCount { get; private set; }
        public int OnCircuitBreakerOpenAditionalHandlerCount { get; private set; }
        public int OnCircuitBreakerCloseAditionalHandlerCount { get; private set; }

        // Constructors
        public ResiliencePolicyWithAllConfig(ILogger<ResiliencePolicyWithAllConfig> logger)
            : base(logger)
        {
            Configure(config => {
                // Identification
                config.Name = nameof(ResiliencePolicyWithAllConfig);
                // Retry
                config.RetryMaxAttemptCount = 5;
                config.RetryAttemptWaitingTimeFunction = (attempt) => TimeSpan.FromMilliseconds(100 * attempt);
                config.OnRetryAditionalHandler = ((int currentRetryCount, TimeSpan retryAttemptWaitingTime, Exception exception) input) => {
                    OnRetryAditionalHandlerCount++;
                };
                // Circuit Breaker
                config.CircuitBreakerWaitingTimeFunction = () => TimeSpan.FromSeconds(3);
                config.OnCircuitBreakerHalfOpenAditionalHandler = () => { OnCircuitBreakerHalfOpenAditionalHandlerCount++; };
                config.OnCircuitBreakerOpenAditionalHandler = ((int currentCircuitBreakerOpenCount, TimeSpan circuitBreakerWaitingTime, Exception exception) input) => {
                    OnCircuitBreakerOpenAditionalHandlerCount++;
                };
                config.OnCircuitBreakerCloseAditionalHandler = () => { OnCircuitBreakerCloseAditionalHandlerCount++; };
                // Exceptions
                config.ExceptionHandleConfigArray = new[] {
                    new Func<Exception, bool>(ex => ex.GetType() == typeof(ArgumentException)),
                    new Func<Exception, bool>(ex => ex.GetType() == typeof(InvalidOperationException))
                };
                // Loggin
                config.IsLoggingEnable = true;
            });
        }

        // Public Methods
        public static new CircuitState GetCircuitState(Polly.CircuitBreaker.CircuitState pollyCircuitState) => ResiliencePolicyBase.GetCircuitState(pollyCircuitState);
    }
    public class ResiliencePolicyWithMinimumConfig
        : ResiliencePolicyBase
    {
        // Constructors
        public ResiliencePolicyWithMinimumConfig(ILogger<ResiliencePolicyWithMinimumConfig> logger)
            : base(logger)
        {
            Configure(config => {
                // Identification
                config.Name = nameof(ResiliencePolicyWithMinimumConfig);
                // Retry
                config.RetryMaxAttemptCount = 5;
                config.RetryAttemptWaitingTimeFunction = (attempt) => TimeSpan.FromMilliseconds(100 * attempt);
                // Circuit Breaker
                config.CircuitBreakerWaitingTimeFunction = () => TimeSpan.FromSeconds(3);
                // Exceptions
                config.ExceptionHandleConfigArray = new[] {
                    new Func<Exception, bool>(ex => ex.GetType() == typeof(ArgumentException)),
                    new Func<Exception, bool>(ex => ex.GetType() == typeof(InvalidOperationException))
                };
            });
        }

        // Public Methods
        public static new CircuitState GetCircuitState(Polly.CircuitBreaker.CircuitState pollyCircuitState) => ResiliencePolicyBase.GetCircuitState(pollyCircuitState);
    }
}
