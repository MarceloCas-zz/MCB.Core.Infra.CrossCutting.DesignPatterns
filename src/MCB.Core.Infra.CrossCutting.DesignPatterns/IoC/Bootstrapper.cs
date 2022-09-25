using Mapster;
using MapsterMapper;
using MCB.Core.Infra.CrossCutting.DependencyInjection.Abstractions.Interfaces;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Adapter;
using MCB.Core.Infra.CrossCutting.DesignPatterns.IoC.Models;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.IoC;

public static class Bootstrapper
{
    // Private Static Methods
    private static void ConfigureServicesForAdapterPattern(IDependencyInjectionContainer dependencyInjectionContainer, Action<AdapterConfig> adapterConfigurationAction)
    {
        if (adapterConfigurationAction is null)
            return;

        var adapterConfig = new AdapterConfig();
        adapterConfigurationAction(adapterConfig);

        dependencyInjectionContainer.Register(
            lifecycle: adapterConfig.DependencyInjectionLifecycle,
            concreteType: typeof(IMapper),
            concreteTypeFactory: dependencyInjectionContainer => new Mapper(adapterConfig.TypeAdapterConfigurationFunction?.Invoke() ?? new TypeAdapterConfig())
        );

        dependencyInjectionContainer.Register(
            lifecycle: adapterConfig.DependencyInjectionLifecycle,
            abstractionType: typeof(IAdapter),
            concreteType: typeof(Adapter.Adapter)
        );
    }

    // Public Static Methods
    public static void ConfigureServices(
        IDependencyInjectionContainer dependencyInjectionContainer,
        Action<AdapterConfig> adapterConfigurationAction
    )
    {
        ConfigureServicesForAdapterPattern(dependencyInjectionContainer, adapterConfigurationAction);
    }
}
