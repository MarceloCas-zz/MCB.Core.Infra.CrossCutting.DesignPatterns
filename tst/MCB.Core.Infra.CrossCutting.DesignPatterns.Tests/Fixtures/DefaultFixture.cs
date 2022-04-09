using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Tests.Fixtures
{
    [CollectionDefinition(nameof(DefaultFixture))]
    public class DefaultFixtureCollection
        : ICollectionFixture<DefaultFixture>
    {

    }
    public class DefaultFixture
    {
        // Properties
        public IServiceProvider ServiceProvider { get; }

        // Constructors
        public DefaultFixture()
        {
            ServiceProvider = ConfigureServices(new ServiceCollection()).BuildServiceProvider();
        }

        // Private Methods
        private IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            return services;
        }
    }
}
