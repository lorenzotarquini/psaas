namespace PSAAS.Application.Contracts.Tenants;

/// <summary>UC1 + UC21 (mock 1c): search, filters, sort and page of the tenant list.</summary>
/// <param name="Statuses">TenantStatus names, OR-ed (UC21-R2); empty = any.</param>
/// <param name="PlanIds">OR-ed; empty = any.</param>
/// <param name="RenewalWithinDays">Renewal (trial end for a trial) due within N days, overdue included.</param>
/// <param name="Ids">Only these tenants (export of a selection).</param>
/// <param name="Sort">One of <see cref="TenantListSorts.All"/>, "-" in front for descending.</param>
public sealed record TenantListQuery(
    string? Search = null,
    IReadOnlyList<string>? Statuses = null,
    IReadOnlyList<Guid>? PlanIds = null,
    string? ModuleId = null,
    string? Province = null,
    int? RenewalWithinDays = null,
    IReadOnlyList<Guid>? Ids = null,
    string Sort = TenantListSorts.CompanyName,
    int Page = 1,
    int PageSize = 20);

public static class TenantListSorts
{
    public const string CompanyName = "companyName";
    public const string City = "city";
    public const string Status = "status";
    public const string Plan = "plan";
    public const string RenewsAt = "renewsAt";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { CompanyName, City, Status, Plan, RenewsAt };

    public static bool IsValid(string sort) => All.Contains(sort.TrimStart('-'));
}

/// <summary>A row of mock 1c.</summary>
/// <param name="RenewsAt">The next renewal, or the end of the trial for a tenant in trial; null without a plan.</param>
/// <param name="UserCount">Users with a profile of the tenant; null when Keycloak could not count them.</param>
public sealed record TenantListItem(
    Guid Id,
    string CompanyName,
    string VatNumber,
    string CompleteVatNumber,
    string CertifiedEmail,
    string City,
    string? Province,
    string Code,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? PlanId,
    string? PlanName,
    string? SubscriptionStatus,
    int? MaxUsers,
    DateTimeOffset? RenewsAt,
    IReadOnlyList<TenantListModule> Modules,
    IReadOnlyList<string> ProfileIds,
    int? UserCount = null);

public sealed record TenantListModule(string Id, string Name, string? Icon, string Status);

/// <summary>UC21-R1: the numbers of the tabs, over every tenant.</summary>
public sealed record TenantCounts(int All, IReadOnlyDictionary<string, int> ByStatus, int RenewingSoon);
