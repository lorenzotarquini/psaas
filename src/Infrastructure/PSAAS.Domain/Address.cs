namespace PSAAS.Domain;

public record Address(string street, string city, string postalCode, string country)
{
    string Street { get; }
    string City { get; }
    string PostalCode { get; }
    string Country { get; }
}