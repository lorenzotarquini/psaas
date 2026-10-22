using Microsoft.EntityFrameworkCore;
using NLog;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using PSAAS.Domain;
using PSAAS.Infrastructure.Catalog;
using PSAAS.Infrastructure.Data.DbContexts;
using PSAAS.Infrastructure.Data.DbModels;

namespace PSAAS.Infrastructure.Repositories.Tenants;

/// <summary>UC21: tenants with their subscription (left join) filtered and sorted in SQL; modules and profiles of the page after.</summary>
public sealed class TenantListRepository(PsaasDbContext dbContext) : ITenantListRepository
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private sealed class Row
    {
        public TenantDbModel Tenant { get; init; } = null!;
        public SubscriptionDbModel? Subscription { get; init; }
    }

    public async Task<PagedResult<TenantListItem>> GetListAsync(TenantListQuery query, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var tenants = dbContext.Tenants.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // UC1-R2, as in TenantReadRepository.
            var normalizedName = TenantFieldNormalizer.NormalizeCompanyName(query.Search);
            var vat = query.Search.Trim().ToUpperInvariant();
            var email = TenantFieldNormalizer.NormalizeCertifiedEmail(query.Search);
            tenants = tenants.Where(tenant =>
                (normalizedName != "" && tenant.NormalizedCompanyName.Contains(normalizedName))
                || tenant.VatNumber.Contains(vat)
                || tenant.CompleteVatNumber.Contains(vat)
                || tenant.NormalizedCertifiedEmail.Contains(email));
        }

        if (query.Statuses is { Count: > 0 } statuses) tenants = tenants.Where(t => statuses.Contains(t.Status));
        if (!string.IsNullOrWhiteSpace(query.Province))
        {
            var province = query.Province.Trim().ToUpperInvariant();
            tenants = tenants.Where(t => t.Address.province == province);
        }

        if (query.Ids is { Count: > 0 })
        {
            var ids = query.Ids.Select(id => id.ToString()).ToArray();
            tenants = tenants.Where(t => ids.Contains(t.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.ModuleId))
        {
            var moduleId = query.ModuleId.Trim();
            tenants = tenants.Where(t => dbContext.TenantModules.Any(m => m.TenantId == t.Id && m.ModuleId == moduleId));
        }

        var rows =
            from t in tenants
            join s in dbContext.Subscriptions on t.Id equals s.TenantId into subscriptions
            from s in subscriptions.DefaultIfEmpty()
            select new Row { Tenant = t, Subscription = s };

        if (query.PlanIds is { Count: > 0 } planIds) rows = rows.Where(r => r.Subscription != null && planIds.Contains(r.Subscription.PlanId));
        if (query.RenewalWithinDays is { } days)
        {
            var limit = now.AddDays(days);
            rows = rows.Where(r => r.Subscription != null && (r.Subscription.TrialEndsAt ?? r.Subscription.RenewsAt) <= limit);
        }

        var totalCount = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
        var page = await Sort(rows, query.Sort)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var pageIds = page.Select(r => r.Tenant.Id).ToArray();
        var modules = (await dbContext.TenantModules.AsNoTracking()
                .Where(m => pageIds.Contains(m.TenantId))
                .Join(dbContext.Modules, tm => tm.ModuleId, m => m.Id, (tm, m) => new { tm.TenantId, m.Id, m.Name, m.Icon, tm.Status, m.IsCore })
                .ToListAsync(cancellationToken).ConfigureAwait(false))
            .ToLookup(m => m.TenantId);
        var profiles = (await dbContext.Profiles.AsNoTracking()
                .Where(p => pageIds.Contains(p.TenantId))
                .Select(p => new { p.TenantId, p.Id })
                .ToListAsync(cancellationToken).ConfigureAwait(false))
            .ToLookup(p => p.TenantId, p => p.Id);

        Logger.Debug("Listed {TenantCount} of {TotalCount} tenants", page.Count, totalCount);
        return new PagedResult<TenantListItem>(
            page.Select(r => new TenantListItem(
                Guid.Parse(r.Tenant.Id),
                r.Tenant.CompanyName,
                r.Tenant.VatNumber,
                r.Tenant.CompleteVatNumber,
                r.Tenant.CertifiedEmail,
                r.Tenant.Address.city,
                r.Tenant.Address.province,
                Tenant.CodeOf(Guid.Parse(r.Tenant.Id)),
                r.Tenant.Status,
                r.Tenant.CreatedAt,
                r.Subscription?.PlanId,
                r.Subscription?.PlanName,
                r.Subscription?.Status,
                r.Subscription?.MaxUsers,
                r.Subscription is null ? null : r.Subscription.TrialEndsAt ?? r.Subscription.RenewsAt,
                // Core first, then by name, as in mock 1c.
                modules[r.Tenant.Id].OrderByDescending(m => m.IsCore).ThenBy(m => m.Name)
                    .Select(m => new TenantListModule(m.Id, m.Name, m.Icon, m.Status)).ToArray(),
                profiles[r.Tenant.Id].ToArray())).ToArray(),
            totalCount,
            query.Page,
            query.PageSize);
    }

    public async Task<TenantCounts> GetCountsAsync(DateTimeOffset now, int renewalDays, CancellationToken cancellationToken = default)
    {
        var byStatus = await dbContext.Tenants.AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken)
            .ConfigureAwait(false);
        var limit = now.AddDays(renewalDays);
        var ceased = nameof(TenantStatus.Ceased);
        var renewing = await (
                from t in dbContext.Tenants
                join s in dbContext.Subscriptions on t.Id equals s.TenantId
                where t.Status != ceased && (s.TrialEndsAt ?? s.RenewsAt) <= limit
                select t.Id)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        return new TenantCounts(byStatus.Values.Sum(), byStatus, renewing);
    }

    private static IQueryable<Row> Sort(IQueryable<Row> rows, string sort)
    {
        var descending = sort.StartsWith('-');
        IOrderedQueryable<Row> ordered = sort.TrimStart('-') switch
        {
            TenantListSorts.City => descending ? rows.OrderByDescending(r => r.Tenant.Address.city) : rows.OrderBy(r => r.Tenant.Address.city),
            TenantListSorts.Status => descending ? rows.OrderByDescending(r => r.Tenant.Status) : rows.OrderBy(r => r.Tenant.Status),
            TenantListSorts.Plan => descending
                ? rows.OrderByDescending(r => r.Subscription == null ? null : r.Subscription.PlanName)
                : rows.OrderBy(r => r.Subscription == null ? null : r.Subscription.PlanName),
            TenantListSorts.RenewsAt => descending
                ? rows.OrderByDescending(r => r.Subscription == null ? (DateTimeOffset?)null : r.Subscription.TrialEndsAt ?? r.Subscription.RenewsAt)
                : rows.OrderBy(r => r.Subscription == null ? (DateTimeOffset?)null : r.Subscription.TrialEndsAt ?? r.Subscription.RenewsAt),
            _ => descending ? rows.OrderByDescending(r => r.Tenant.CompanyName) : rows.OrderBy(r => r.Tenant.CompanyName),
        };
        // UC21-R4: company name, then id, keep pages stable.
        return ordered.ThenBy(r => r.Tenant.CompanyName).ThenBy(r => r.Tenant.Id);
    }
}
