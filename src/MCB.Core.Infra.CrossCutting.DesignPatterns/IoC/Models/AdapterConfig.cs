using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.IoC.Models
{
    public class AdapterConfig
    {
        // Properties
        public Func<TypeAdapterConfig> TypeAdapterConfigurationFunction { get; set; }
        public ServiceLifetime AdapterServiceLifetime { get; set; }

        // Constructors
        public AdapterConfig()
        {
            AdapterServiceLifetime = ServiceLifetime.Singleton;
        }
    }
}
