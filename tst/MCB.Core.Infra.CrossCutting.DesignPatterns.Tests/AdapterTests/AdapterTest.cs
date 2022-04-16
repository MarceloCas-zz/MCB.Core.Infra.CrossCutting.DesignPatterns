#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

using FluentAssertions;
using Mapster;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Adapters;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Tests.AdapterTests.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Tests.AdapterTests
{
    public class AdapterTest
    {
        [Fact]
        public void Adapter_Shoul_Be_Adapt_Correctly()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            IoC.Bootstrapper.ConfigureServices(
                serviceCollection,
                adapterConfiguration =>
                {
                    adapterConfiguration.AdapterServiceLifetime = ServiceLifetime.Singleton;
                    adapterConfiguration.TypeAdapterConfigurationFunction = new Func<TypeAdapterConfig>(() =>
                    {
                        var typeAdapterConfig = new TypeAdapterConfig();

                        typeAdapterConfig.ForType<AddressDto, Address>();

                        return typeAdapterConfig;
                    });
                }
            );
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var adapter = serviceProvider.GetService<IAdapter>();

            if (adapter == null)
            {
                Assert.False(false);
                return;
            }

            var id = Guid.NewGuid();

            var addressDto = new AddressDto
            {
                Id = id,
                City = "São Paulo",
                Neighborhood = "Se",
                Number = "N/A",
                Street = "Praça da Sé",
                ZipCode = "01001-000"
            };

            // Act
            var address = adapter.Adapt<AddressDto, Address>(addressDto) ?? new Address();
            var address2 = adapter.Adapt<AddressDto, Address>(addressDto, existingTarget: default) ?? new Address();

            // Assert
            address.Id.Should().Be(id);
            address.City.Should().Be(addressDto.City);
            address.Neighborhood.Should().Be(addressDto.Neighborhood);
            address.Number.Should().Be(addressDto.Number);
            address.Street.Should().Be(addressDto.Street);
            address.ZipCode.Should().Be(addressDto.ZipCode);

            address2.Id.Should().Be(id);
            address2.Neighborhood.Should().Be(addressDto.Neighborhood);
            address2.Number.Should().Be(addressDto.Number);
            address2.Street.Should().Be(addressDto.Street);
            address2.ZipCode.Should().Be(addressDto.ZipCode);

        }

        [Fact]
        public void Adapter_Shoul_Be_Adapt_Correctly_With_Existing_Target()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            IoC.Bootstrapper.ConfigureServices(
                serviceCollection,
                adapterConfiguration =>
                {
                    adapterConfiguration.AdapterServiceLifetime = ServiceLifetime.Singleton;
                    adapterConfiguration.TypeAdapterConfigurationFunction = new Func<TypeAdapterConfig>(() =>
                    {
                        var typeAdapterConfig = new TypeAdapterConfig();

                        typeAdapterConfig.ForType<AddressDto, Address>();

                        return typeAdapterConfig;
                    });
                }
            );
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var adapter = serviceProvider.GetService<IAdapter>();

            if (adapter == null)
            {
                Assert.False(false);
                return;
            }

            var aditionalAddressProperty = 1;
            var addressDto = new AddressDto
            {
                City = "São Paulo",
                Neighborhood = "Se",
                Number = "N/A",
                Street = "Praça da Sé",
                ZipCode = "01001-000"
            };

            // Act
            var address = new Address { AditionalAddressProperty = aditionalAddressProperty };
            address = adapter.Adapt(addressDto, address) ?? new Address();

            // Assert
            address.AditionalAddressProperty.Should().Be(aditionalAddressProperty);
            address.City.Should().Be(addressDto.City);
            address.Neighborhood.Should().Be(addressDto.Neighborhood);
            address.Number.Should().Be(addressDto.Number);
            address.Street.Should().Be(addressDto.Street);
            address.ZipCode.Should().Be(addressDto.ZipCode);
        }
    }
}

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
