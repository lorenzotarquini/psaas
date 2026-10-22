namespace PSAAS.Domain;

/// <param name="province">Province code of the city (e.g. "BO"), from the city list when the address was saved (UC21-R3).</param>
public record Address(string street, string city, string postalCode, string country, string? province = null)
{
    string Street { get; }
    string City { get; }
    string PostalCode { get; }
    string Country { get; }
}