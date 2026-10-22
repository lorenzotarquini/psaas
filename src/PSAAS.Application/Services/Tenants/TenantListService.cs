using System.Globalization;
using System.Text;
using NLog;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Tenants;

namespace PSAAS.Application.Services.Tenants;

/// <summary>UC21 (mock 1c): filtered tenant list with users against the plan limit, tab counts and CSV export.</summary>
public sealed class TenantListService(ITenantListRepository repository, IUserDirectory users, TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>UC21-R1: "Rinnovo &lt; 30 gg".</summary>
    public const int RenewalSoonDays = 30;

    public const int MaxExportRows = 5000;

    /// <summary>The field of the first invalid parameter, or null when the query is valid.</summary>
    public static string? InvalidParameter(TenantListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!TenantListSorts.IsValid(query.Sort)) return "sort";
        if (query.Statuses?.Any(status => !Enum.TryParse<Domain.TenantStatus>(status, out _)) == true) return "status";
        if (query.RenewalWithinDays is < 1 or > 3650) return "renewalWithinDays";
        return null;
    }

    public async Task<PagedResult<TenantListItem>> GetListAsync(TenantListQuery query, CancellationToken cancellationToken = default)
    {
        var page = await repository.GetListAsync(query, clock.GetUtcNow(), cancellationToken);
        var items = new List<TenantListItem>(page.Items.Count);
        try
        {
            // shortcut: one Keycloak count per tenant of the page (each one search per profile); fine for pages of 20–100.
            foreach (var item in page.Items)
            {
                items.Add(item with { UserCount = item.ProfileIds.Count == 0 ? 0 : await users.CountUsersWithAnyAsync(item.ProfileIds, cancellationToken) });
            }
        }
        catch (UserDirectoryUnavailableException exception)
        {
            // UC21 3a: the list still shows, without the user counts.
            Logger.Warn(exception, "Tenant list shown without user counts: Keycloak unreachable");
            items = page.Items.ToList();
        }

        Logger.Info("Listed {TenantCount} of {TotalCount} tenants", items.Count, page.TotalCount);
        return page with { Items = items };
    }

    public Task<TenantCounts> GetCountsAsync(CancellationToken cancellationToken = default) =>
        repository.GetCountsAsync(clock.GetUtcNow(), RenewalSoonDays, cancellationToken);

    /// <summary>UC21-R5: the filtered tenants as CSV (no Keycloak calls: no user counts).</summary>
    public async Task<string> ExportCsvAsync(TenantListQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await repository.GetListAsync(query with { Page = 1, PageSize = MaxExportRows }, clock.GetUtcNow(), cancellationToken);
        var csv = new StringBuilder("ragioneSociale,partitaIva,pec,comune,provincia,stato,piano,rinnovo\n");
        foreach (var t in rows.Items)
        {
            csv.AppendJoin(',', new[]
            {
                t.CompanyName, t.CompleteVatNumber, t.CertifiedEmail, t.City, t.Province ?? string.Empty, t.Status, t.PlanName ?? string.Empty,
                t.RenewsAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            }.Select(Quote)).Append('\n');
        }

        Logger.Info("Exported {TenantCount} tenants", rows.Items.Count);
        return csv.ToString();
    }

    // RFC 4180, and a leading = + - @ neutralized so a spreadsheet does not run it as a formula.
    private static string Quote(string value)
    {
        if (value.Length > 0 && "=+-@".Contains(value[0], StringComparison.Ordinal)) value = "'" + value;
        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
    }
}
