using FluentAssertions;
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
            IoC.Bootstrapper.ConfigureServices(serviceCollection, mapperConfiguration => {
                mapperConfiguration.CreateMap<AddressDto, Address>();
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var adapter = serviceProvider.GetService<IAdapter>();

            if (adapter == null)
            {
                Assert.False(false);
                return;
            }

            var addressDto = new AddressDto
            {
                City = "São Paulo",
                Neighborhood = "Se",
                Number = "N/A",
                Street = "Praça da Sé",
                ZipCode = "01001-000"
            };

            // Act
            var address = adapter.Adapt<AddressDto, Address>(addressDto);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var address2 = adapter.Adapt<AddressDto, Address>(addressDto, existingTarget: null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            // Assert
            address.City.Should().Be(addressDto.City);
            address.Neighborhood.Should().Be(addressDto.Neighborhood);
            address.Number.Should().Be(addressDto.Number);
            address.Street.Should().Be(addressDto.Street);
            address.ZipCode.Should().Be(addressDto.ZipCode);

            address2.City.Should().Be(addressDto.City);
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
            IoC.Bootstrapper.ConfigureServices(serviceCollection, mapperConfiguration => {
                mapperConfiguration.CreateMap<AddressDto, Address>();
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var adapter = serviceProvider.GetService<IAdapter>();
            
            if (adapter == null)
            {
                Assert.False(false);
                return;
            }

            var addressId = Guid.NewGuid();
            var addressDto = new AddressDto
            {
                City = "São Paulo",
                Neighborhood = "Se",
                Number = "N/A",
                Street = "Praça da Sé",
                ZipCode = "01001-000"
            };

            // Act
            var address = new Address { Id = addressId };
            address = adapter.Adapt(addressDto, address);

            // Assert
            address.Id.Should().Be(addressId);
            address.City.Should().Be(addressDto.City);
            address.Neighborhood.Should().Be(addressDto.Neighborhood);
            address.Number.Should().Be(addressDto.Number);
            address.Street.Should().Be(addressDto.Street);
            address.ZipCode.Should().Be(addressDto.ZipCode);
        }
    }
}
