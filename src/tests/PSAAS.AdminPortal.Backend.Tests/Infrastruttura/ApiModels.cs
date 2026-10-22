namespace PSAAS.AdminPortal.Backend.Tests.Infrastruttura;

// The API as a client sees it (docs/use-cases/API.md): deliberately independent from the backend types.

public sealed record PageView<T>(List<T> Items, int TotalCount, int Page, int PageSize);

public sealed record TenantSummary(
    Guid Id, string CompanyName, string VatNumber, string CompleteVatNumber, string CertifiedEmail, string City,
    string Code = "", string Status = "", DateTimeOffset CreatedAt = default);

public sealed record TenantDetail(
    Guid Id,
    string CompanyName,
    string VatNumber,
    string CompleteVatNumber,
    string CertifiedEmail,
    AddressView Address,
    List<ProfileView> Profiles,
    List<ProductiveUnitView> ProductiveUnits,
    string Code = "",
    LifecycleView? Lifecycle = null,
    string? SdiCode = null,
    string? AccountManager = null)
{
    public ProfileView Profile(string name) => Profiles.Single(profile => profile.Name == name);

    public ProductiveUnitView Unit(string name) => ProductiveUnits.Single(unit => unit.Name == name);
}

public sealed record AddressView(string Street, string City, string PostalCode, string Country);

public sealed record ProfileView(string Id, string Name, string? Description, int? UserCount = null);

public sealed record ProductiveUnitView(Guid Id, string Name, string? Description, List<string> ProfileIds);

public sealed record ProfileOverview(string Id, string TenantId, string TenantCompanyName, string Name, string? Description);

public sealed record CityView(string Name, string ProvinceCode, List<string> PostalCodes);

public sealed record MeView(string? KeycloakId, string? Name, string? Email, List<string> Roles);

public sealed record UserView(
    string Id, string FirstName, string LastName, string Email, DateOnly BirthDate, List<UserProfileView> Profiles,
    string Status = "Active", bool MfaRequired = false, DateTimeOffset? LastLoginAt = null, string? CreatedBy = null, int InvitesSent = 0);

public sealed record UserProfileView(string Id, string Name, string TenantId, string TenantCompanyName);

public sealed record FieldValidationView(string? ErrorCode, TenantReferenceView? ConflictingTenant = null);

public sealed record TenantReferenceView(Guid Id, string CompanyName);

public sealed record AuditEventView(
    Guid Id,
    DateTimeOffset OccurredAt,
    string ActorId,
    string ActorName,
    string Action,
    string TargetType,
    string TargetId,
    string TargetName,
    string? TenantId,
    System.Text.Json.JsonElement? Details);

public sealed record LifecycleView(string Status, string? Reason, DateTimeOffset ChangedAt, DateTimeOffset CreatedAt);

public sealed record ModuleView(string Id, string Name, string Status, string Version, bool IsCore, List<string> Requires, List<string> Permissions, int TenantCount);

public sealed record PlanView(Guid Id, string Code, string Name, decimal Price, string BillingPeriod, int TrialDays, int? MaxUsers, List<string> ModuleIds, bool Active);

public sealed record SubscriptionView(
    Guid TenantId, Guid PlanId, string PlanName, string Status, decimal Price, int? MaxUsers,
    DateTimeOffset StartsAt, DateTimeOffset? TrialEndsAt, DateTimeOffset RenewsAt, List<TenantModuleView> Modules)
{
    public TenantModuleView Module(string id) => Modules.Single(module => module.ModuleId == id);
}

public sealed record TenantModuleView(string ModuleId, string Status, int Attempts, string? LastError);

public sealed record ImportRowView(int Row, string Email, Dictionary<string, string> Errors, UserView? Created);
