using Microsoft.EntityFrameworkCore;
using NLog;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using PSAAS.Domain;
using PSAAS.Infrastructure.Converters;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Repositories.Tenants;

public sealed class TenantReadRepository(PsaasDbContext dbContext) : ITenantReadRepository
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<PagedResult<Tenant>> GetPageAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        Logger.Debug("Querying tenants page {Page} (size {PageSize}, search {Search})", page, pageSize, search);
        var query = dbContext.Tenants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedName = TenantFieldNormalizer.NormalizeCompanyName(search);
            var vat = search.Trim().ToUpperInvariant();
            // UC1-R2: certified emails are stored normalized (uppercase), so the search ignores case.
            var email = TenantFieldNormalizer.NormalizeCertifiedEmail(search);
            query = query.Where(tenant =>
                (normalizedName != "" && tenant.NormalizedCompanyName.Contains(normalizedName))
                || tenant.VatNumber.Contains(vat)
                || tenant.CompleteVatNumber.Contains(vat)
                || tenant.NormalizedCertifiedEmail.Contains(email));
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var tenants = await query
            .OrderBy(tenant => tenant.CompanyName)
            .ThenBy(tenant => tenant.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Logger.Debug("Queried {TenantCount} of {TotalCount} tenants", tenants.Count, totalCount);

        return new PagedResult<Tenant>(
            tenants.Select(TenantDbModelConverter.ToDomain).ToArray(),
            totalCount,
            page,
            pageSize);
    }

    public async Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        Logger.Debug("Querying tenant {TenantId}", tenantId);
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(
                tenant => tenant.Id == tenantId.ToString(),
                cancellationToken)
            .ConfigureAwait(false);

        return tenant is null ? null : TenantDbModelConverter.ToDomain(tenant);
    }

    public async Task<TenantReference?> FindByUniqueValueAsync(
        string field,
        string normalizedValue,
        Guid? excludedTenantId,
        CancellationToken cancellationToken = default)
    {
        var excluded = excludedTenantId?.ToString();
        var query = dbContext.Tenants.AsNoTracking().Where(tenant => tenant.Id != excluded);
        query = field switch
        {
            "companyName" => query.Where(tenant => tenant.NormalizedCompanyName == normalizedValue),
            "completeVatNumber" => query.Where(tenant => tenant.CompleteVatNumber == normalizedValue),
            "certifiedEmail" => query.Where(tenant => tenant.NormalizedCertifiedEmail == normalizedValue),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Not a unique tenant field")
        };

        var found = await query
            .Select(tenant => new { tenant.Id, tenant.CompanyName })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return found is null ? null : new TenantReference(Guid.Parse(found.Id), found.CompanyName);
    }

    public async Task<TenantStructureReadModel> GetProfilesAndProductiveUnitsAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        Logger.Debug("Querying structure of tenant {TenantId}", tenantId);
        var profiles = await dbContext.Profiles
            .AsNoTracking()
            .Where(profile => profile.TenantId == tenantId)
            .OrderBy(profile => profile.Name)
            .Select(profile => new TenantProfileReadModel(profile.Id, profile.Name, profile.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var units = await dbContext.ProductiveUnits
            .AsNoTracking()
            .Where(unit => unit.TenantId == tenantId)
            .OrderBy(unit => unit.Name)
            .Select(unit => new
            {
                unit.Id,
                unit.Name,
                unit.Description,
                ProfileIds = unit.ProductiveUnitProfiles.Select(link => link.ProfileId).ToList()
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new TenantStructureReadModel(
            profiles,
            units.Select(unit => new TenantProductiveUnitReadModel(unit.Id, unit.Name, unit.Description, unit.ProfileIds)).ToList());
    }
}
