using AutoMapper;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Adapters;
using Microsoft.Extensions.DependencyInjection;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.IoC
{
    public static class Bootstrapper
    {
        public static void ConfigureServices(
            IServiceCollection services,
            Action<IMapperConfigurationExpression> mapperConfiguration
        )
        {
            services.AddAutoMapper(q => mapperConfiguration(q));
            services.AddSingleton<IAdapter, Adapter.Adapter>();
        }
    }
}
