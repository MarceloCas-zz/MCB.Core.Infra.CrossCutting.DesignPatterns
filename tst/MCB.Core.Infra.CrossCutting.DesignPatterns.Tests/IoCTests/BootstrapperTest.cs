﻿using FluentAssertions;
using Mapster;
using MapsterMapper;
using MCB.Core.Infra.CrossCutting.DependencyInjection;
using MCB.Core.Infra.CrossCutting.DependencyInjection.Abstractions.Enums;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Adapter;
using MCB.Core.Infra.CrossCutting.DesignPatterns.IoC.Models;
using System;
using System.Linq;
using Xunit;


namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Tests.IoCTests;

public class BootstrapperTest
{
    [Fact]
    public void Bootstrapper_Should_Configure_For_Adapter_Patterns()
    {
        // Arrange
        var dependencyInjectionContainer = new DependencyInjectionContainer();
        var adapterConfigAux = default(AdapterConfig);

        // Act
        IoC.Bootstrapper.ConfigureServices(dependencyInjectionContainer, adapterConfig => { adapterConfigAux = adapterConfig; });
        dependencyInjectionContainer.Build();

        // Assert
        var registrationCollection = dependencyInjectionContainer.GetRegistrationCollection().ToList();
        var mapperRegistration = registrationCollection.FirstOrDefault(q => q.AbstractionType == typeof(IMapper));
        var adapterRegistration = registrationCollection.FirstOrDefault(q => q.AbstractionType == typeof(IAdapter));
        var mapper = dependencyInjectionContainer.Resolve<IMapper>();
        var adapter = dependencyInjectionContainer.Resolve<IAdapter>();

        mapperRegistration.Should().NotBeNull();
        mapperRegistration.DependencyInjectionLifecycle.Should().Be(adapterConfigAux.DependencyInjectionLifecycle);
        adapterRegistration.Should().NotBeNull();
        adapterRegistration.DependencyInjectionLifecycle.Should().Be(adapterConfigAux.DependencyInjectionLifecycle);
        mapper.Should().NotBeNull();
        adapter.Should().NotBeNull();

        adapterConfigAux.TypeAdapterConfigurationFunction.Should().BeNull();
    }

    [Fact]
    public void Bootstrapper_Should_Configure_With_Config_For_Adapter_Patterns()
    {
        // Arrange
        var dependencyInjectionContainer = new DependencyInjectionContainer();
        var adapterConfigAux = default(AdapterConfig);

        // Act
        IoC.Bootstrapper.ConfigureServices(dependencyInjectionContainer, adapterConfig => { 
            adapterConfig.DependencyInjectionLifecycle = DependencyInjectionLifecycle.Scoped;
            adapterConfig.TypeAdapterConfigurationFunction = new Func<TypeAdapterConfig>(() => { return new TypeAdapterConfig(); });
            adapterConfigAux = adapterConfig;
        });
        dependencyInjectionContainer.Build();

        // Assert
        var registrationCollection = dependencyInjectionContainer.GetRegistrationCollection();
        var mapperRegistration = registrationCollection.FirstOrDefault(q => q.AbstractionType == typeof(IMapper));
        var adapterRegistration = registrationCollection.FirstOrDefault(q => q.AbstractionType == typeof(IAdapter));
        var mapper = dependencyInjectionContainer.Resolve<IMapper>();
        var adapter = dependencyInjectionContainer.Resolve<IAdapter>();

        mapperRegistration.Should().NotBeNull();
        mapperRegistration.DependencyInjectionLifecycle.Should().Be(adapterConfigAux.DependencyInjectionLifecycle);
        adapterRegistration.Should().NotBeNull();
        adapterRegistration.DependencyInjectionLifecycle.Should().Be(adapterConfigAux.DependencyInjectionLifecycle);
        mapper.Should().NotBeNull();
        adapter.Should().NotBeNull();

        adapterConfigAux.TypeAdapterConfigurationFunction.Should().NotBeNull();
    }

    [Fact]
    public void Bootstrapper_Should_Not_Configure_For_Adapter_Patterns()
    {
        // Arrange
        var dependencyInjectionContainer = new DependencyInjectionContainer();

        // Act
        IoC.Bootstrapper.ConfigureServices(dependencyInjectionContainer, adapterConfigurationAction: null);
        dependencyInjectionContainer.Build();

        // Assert
        var registrationCollection = dependencyInjectionContainer.GetRegistrationCollection();
        var mapperRegistration = registrationCollection.FirstOrDefault(q => q.AbstractionType == typeof(IMapper));
        var adapterRegistration = registrationCollection.FirstOrDefault(q => q.AbstractionType == typeof(IAdapter));
        var mapper = dependencyInjectionContainer.Resolve<IMapper>();
        var adapter = dependencyInjectionContainer.Resolve<IAdapter>();

        mapperRegistration.Should().BeNull();
        adapterRegistration.Should().BeNull();
        mapper.Should().BeNull();
        adapter.Should().BeNull();
    }
}
