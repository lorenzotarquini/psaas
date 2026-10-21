namespace PSAAS.AdminPortal.Backend.Tests.Infrastruttura;

// The API as a client sees it (docs/use-cases/API.md): deliberately independent from the backend types.

public sealed record PageView<T>(List<T> Items, int TotalCount, int Page, int PageSize);

public sealed record TenantSummary(
    Guid Id, string CompanyName, string VatNumber, string CompleteVatNumber, string CertifiedEmail, string City);

public sealed record TenantDetail(
    Guid Id,
    string CompanyName,
    string VatNumber,
    string CompleteVatNumber,
    string CertifiedEmail,
    AddressView Address,
    List<ProfileView> Profiles,
    List<ProductiveUnitView> ProductiveUnits)
{
    public ProfileView Profile(string name) => Profiles.Single(profile => profile.Name == name);

    public ProductiveUnitView Unit(string name) => ProductiveUnits.Single(unit => unit.Name == name);
}

public sealed record AddressView(string Street, string City, string PostalCode, string Country);

public sealed record ProfileView(string Id, string Name, string? Description);

public sealed record ProductiveUnitView(Guid Id, string Name, string? Description, List<string> ProfileIds);

public sealed record ProfileOverview(string Id, string TenantId, string Name, string? Description);

public sealed record CityView(string Name, string ProvinceCode, List<string> PostalCodes);

public sealed record MeView(string? KeycloakId, string? Name, string? Email, List<string> Roles);

public sealed record UserView(
    string Id, string FirstName, string LastName, string Email, DateOnly BirthDate, List<UserProfileView> Profiles);

public sealed record UserProfileView(string Id, string Name, string TenantId, string TenantCompanyName);
