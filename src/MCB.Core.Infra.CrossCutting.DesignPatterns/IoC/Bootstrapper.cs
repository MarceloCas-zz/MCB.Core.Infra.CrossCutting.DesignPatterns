using Mapster;
using MapsterMapper;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Adapter;
using MCB.Core.Infra.CrossCutting.DesignPatterns.IoC.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.IoC
{
    public static class Bootstrapper
    {
        // Private Static Methods
        private static void ConfigureServicesForAdapterPattern(IServiceCollection services, Action<AdapterConfig> adapterConfigurationAction)
        {
            if (adapterConfigurationAction is null)
                return;

            var adapterConfig = new AdapterConfig();
            adapterConfigurationAction(adapterConfig);

            services.Add(
                new ServiceDescriptor(
                    serviceType: typeof(IMapper),
                    factory: serviceProvider => new Mapper(adapterConfig.TypeAdapterConfigurationFunction?.Invoke() ?? new TypeAdapterConfig()),
                    lifetime: adapterConfig.AdapterServiceLifetime
                )
            );
            services.Add(
                new ServiceDescriptor(
                    serviceType: typeof(IAdapter),
                    implementationType: typeof(Adapter.Adapter),
                    lifetime: adapterConfig.AdapterServiceLifetime
                )
            );
        }

        // Public Static Methods
        public static void ConfigureServices(
            IServiceCollection services,
            Action<AdapterConfig> adapterConfigurationAction
        )
        {
            ConfigureServicesForAdapterPattern(services, adapterConfigurationAction);
        }
    }
}
