using System;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Tests.AdapterTests.Models
{
    public class Address
    {
        public Guid? Id { get; set; }
        public string? Street { get; set; }
        public string? Number { get; set; }
        public string? ZipCode { get; set; }
        public string? Neighborhood { get; set; }
        public string? City { get; set; }
    }
}
