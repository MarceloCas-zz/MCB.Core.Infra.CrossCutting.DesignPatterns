using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.IoC
{
    public static class Bootstrapper
    {
        public static void ConfigureServices(
            IServiceCollection services,
            Action<TypeAdapterConfig> mapperConfiguration,
            ServiceLifetime mapperServiceLifetime
        )
        {
            services.Add(
                new ServiceDescriptor(
                    serviceType: typeof(IMapper),
                    factory: serviceProvider => {
                        var typeAdapterConfig = new TypeAdapterConfig();
                        mapperConfiguration(typeAdapterConfig);
                        return new Mapper(typeAdapterConfig);
                    },
                    lifetime: mapperServiceLifetime
                )
            );
        }
    }
}
