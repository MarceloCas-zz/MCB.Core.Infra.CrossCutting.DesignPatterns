#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

using FluentAssertions;
using Mapster;
using MapsterMapper;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Adapter;
using MCB.Core.Infra.CrossCutting.DesignPatterns.IoC.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;


namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Tests.IoCTests
{
    public class BootstrapperTest
    {
        [Fact]
        public void Bootstrapper_Should_Configure_For_Adapter_Patterns()
        {
            // Arrange
            var services = new ServiceCollection();
            var adapterConfigAux = default(AdapterConfig);

            // Act
            IoC.Bootstrapper.ConfigureServices(services, adapterConfig => { adapterConfigAux = adapterConfig; });
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var mapperRegistration = services.FirstOrDefault(q => q.ServiceType == typeof(IMapper));
            var adapterRegistration = services.FirstOrDefault(q => q.ServiceType == typeof(IAdapter));
            var mapper = serviceProvider.GetService<IMapper>();
            var adapter = serviceProvider.GetService<IAdapter>();

            mapperRegistration.Should().NotBeNull();
            mapperRegistration.Lifetime.Should().Be(adapterConfigAux.AdapterServiceLifetime);
            adapterRegistration.Should().NotBeNull();
            adapterRegistration.Lifetime.Should().Be(adapterConfigAux.AdapterServiceLifetime);
            mapper.Should().NotBeNull();
            adapter.Should().NotBeNull();

            adapterConfigAux.TypeAdapterConfigurationFunction.Should().BeNull();
        }

        [Fact]
        public void Bootstrapper_Should_Configure_With_Config_For_Adapter_Patterns()
        {
            // Arrange
            var services = new ServiceCollection();
            var adapterConfigAux = default(AdapterConfig);

            // Act
            IoC.Bootstrapper.ConfigureServices(services, adapterConfig => { 
                adapterConfig.AdapterServiceLifetime = ServiceLifetime.Scoped;
                adapterConfig.TypeAdapterConfigurationFunction = new Func<TypeAdapterConfig>(() => { return new TypeAdapterConfig(); });
                adapterConfigAux = adapterConfig;
            });
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var mapperRegistration = services.FirstOrDefault(q => q.ServiceType == typeof(IMapper));
            var adapterRegistration = services.FirstOrDefault(q => q.ServiceType == typeof(IAdapter));
            var mapper = serviceProvider.GetService<IMapper>();
            var adapter = serviceProvider.GetService<IAdapter>();

            mapperRegistration.Should().NotBeNull();
            mapperRegistration.Lifetime.Should().Be(adapterConfigAux.AdapterServiceLifetime);
            adapterRegistration.Should().NotBeNull();
            adapterRegistration.Lifetime.Should().Be(adapterConfigAux.AdapterServiceLifetime);
            mapper.Should().NotBeNull();
            adapter.Should().NotBeNull();

            adapterConfigAux.TypeAdapterConfigurationFunction.Should().NotBeNull();
        }

        [Fact]
        public void Bootstrapper_Should_Not_Configure_For_Adapter_Patterns()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            IoC.Bootstrapper.ConfigureServices(services, adapterConfigurationAction: null);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var mapperRegistration = services.FirstOrDefault(q => q.ServiceType == typeof(IMapper));
            var adapterRegistration = services.FirstOrDefault(q => q.ServiceType == typeof(IAdapter));
            var mapper = serviceProvider.GetService<IMapper>();
            var adapter = serviceProvider.GetService<IAdapter>();

            mapperRegistration.Should().BeNull();
            adapterRegistration.Should().BeNull();
            mapper.Should().BeNull();
            adapter.Should().BeNull();
        }
    }
}

#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
