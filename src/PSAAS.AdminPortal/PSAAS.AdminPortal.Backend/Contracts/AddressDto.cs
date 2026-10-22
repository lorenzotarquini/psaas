using PSAAS.Domain;

namespace PSAAS.AdminPortal.Backend.Contracts;

public sealed record AddressDto(string Street, string City, string PostalCode, string Country, string? Province)
{
    public static AddressDto From(Address address) =>
        new(address.street, address.city, address.postalCode, address.country, address.province);
}
