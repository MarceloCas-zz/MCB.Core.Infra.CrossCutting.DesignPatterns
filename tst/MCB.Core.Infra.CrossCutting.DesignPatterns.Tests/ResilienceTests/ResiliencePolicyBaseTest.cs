using FluentAssertions;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Resilience;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            resiliencePolicyWithAllConfig.CircuitState.Should().Be(Abstractions.Resilience.Enums.CircuitState.Closed);
            resiliencePolicyWithAllConfig.CurrentCircuitBreakerOpenCount.Should().Be(0);
            resiliencePolicyWithAllConfig.CurrentRetryCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerCloseAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerHalfOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerResetOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnRetryAditionalHandlerCount.Should().Be(0);

            successOnRunResiliencePolicyWithMinimumConfig.Should().BeTrue();
            resiliencePolicyWithMinimumConfig.CircuitState.Should().Be(Abstractions.Resilience.Enums.CircuitState.Closed);
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
            successOnRunResiliencePolicyWithAllConfig = await resiliencePolicyWithAllConfig.ExecuteAsync(() =>
            {
                throw new ArgumentException();
            });

            successOnRunResiliencePolicyWithMinimumConfig = await resiliencePolicyWithMinimumConfig.ExecuteAsync(() =>
            {
                throw new ArgumentException();
            });

            // Assert
            resiliencePolicyWithAllConfig.CircuitState.Should().Be(Abstractions.Resilience.Enums.CircuitState.Open);
            resiliencePolicyWithAllConfig.CurrentCircuitBreakerOpenCount.Should().Be(1);
            resiliencePolicyWithAllConfig.CurrentRetryCount.Should().Be(resiliencePolicyWithAllConfig.ResilienceConfig.RetryMaxAttemptCount);
            resiliencePolicyWithAllConfig.OnCircuitBreakerCloseAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerHalfOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerOpenAditionalHandlerCount.Should().Be(1);
            resiliencePolicyWithAllConfig.OnCircuitBreakerResetOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnRetryAditionalHandlerCount.Should().Be(resiliencePolicyWithAllConfig.ResilienceConfig.RetryMaxAttemptCount);

            resiliencePolicyWithMinimumConfig.CircuitState.Should().Be(Abstractions.Resilience.Enums.CircuitState.Open);
            resiliencePolicyWithMinimumConfig.CurrentCircuitBreakerOpenCount.Should().Be(1);
            resiliencePolicyWithMinimumConfig.CurrentRetryCount.Should().Be(resiliencePolicyWithMinimumConfig.ResilienceConfig.RetryMaxAttemptCount);
        }

        [Fact]
        public async Task ResiliencePolicy_Should_Be_Closed_When_Throw_Exception_Not_Handled()
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
            resiliencePolicyWithAllConfig.CircuitState.Should().Be(Abstractions.Resilience.Enums.CircuitState.Closed);
            resiliencePolicyWithAllConfig.CurrentCircuitBreakerOpenCount.Should().Be(0);
            resiliencePolicyWithAllConfig.CurrentRetryCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerCloseAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerHalfOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnCircuitBreakerResetOpenAditionalHandlerCount.Should().Be(0);
            resiliencePolicyWithAllConfig.OnRetryAditionalHandlerCount.Should().Be(0);
        }
    }

    public class ResiliencePolicyWithAllConfig
        : ResiliencePolicyBase
    {
        public int OnRetryAditionalHandlerCount { get; private set; }
        public int OnCircuitBreakerCloseAditionalHandlerCount { get; private set; }
        public int OnCircuitBreakerHalfOpenAditionalHandlerCount { get; private set; }
        public int OnCircuitBreakerOpenAditionalHandlerCount { get; private set; }
        public int OnCircuitBreakerResetOpenAditionalHandlerCount { get; private set; }

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
                config.OnCircuitBreakerCloseAditionalHandler = () => { OnCircuitBreakerCloseAditionalHandlerCount++; };
                config.OnCircuitBreakerHalfOpenAditionalHandler = () => { OnCircuitBreakerHalfOpenAditionalHandlerCount++; };
                config.OnCircuitBreakerOpenAditionalHandler = ((int currentCircuitBreakerOpenCount, TimeSpan circuitBreakerWaitingTime, Exception exception) input) => {
                    OnCircuitBreakerOpenAditionalHandlerCount++;
                };
                config.OnCircuitBreakerResetOpenAditionalHandler = () => { OnCircuitBreakerResetOpenAditionalHandlerCount++; };
                // Exceptions
                config.ExceptionHandleConfigArray = new[] {
                    new Func<Exception, bool>(ex => ex.GetType() == typeof(ArgumentException)),
                    new Func<Exception, bool>(ex => ex.GetType() == typeof(InvalidOperationException))
                };
                // Loggin
                config.IsLoggingEnable = true;
            });
        }
    }
    public class ResiliencePolicyWithMinimumConfig
        : ResiliencePolicyBase
    {
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
    }
}
