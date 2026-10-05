using Microsoft.EntityFrameworkCore;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Repositories.Tenants;

public sealed class TenantUniquenessChecker(PsaasDbContext dbContext) : ITenantUniquenessChecker
{
    public async Task<bool> IsCompanyNameUniqueAsync(
        string normalizedCompanyName,
        CancellationToken cancellationToken = default)
    {
        return !await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.NormalizedCompanyName == normalizedCompanyName,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsCompanyNameUniqueAsync(
        string normalizedCompanyName,
        Guid excludedTenantId,
        CancellationToken cancellationToken = default)
    {
        return !await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.NormalizedCompanyName == normalizedCompanyName
                    && tenant.Id != excludedTenantId.ToString(),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsCompleteVatNumberUniqueAsync(
        string normalizedCompleteVatNumber,
        CancellationToken cancellationToken = default)
    {
        return !await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.CompleteVatNumber == normalizedCompleteVatNumber,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsCompleteVatNumberUniqueAsync(
        string normalizedCompleteVatNumber,
        Guid excludedTenantId,
        CancellationToken cancellationToken = default)
    {
        return !await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.CompleteVatNumber == normalizedCompleteVatNumber
                    && tenant.Id != excludedTenantId.ToString(),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsCertifiedEmailUniqueAsync(
        string normalizedCertifiedEmail,
        CancellationToken cancellationToken = default)
    {
        return !await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.NormalizedCertifiedEmail == normalizedCertifiedEmail,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsCertifiedEmailUniqueAsync(
        string normalizedCertifiedEmail,
        Guid excludedTenantId,
        CancellationToken cancellationToken = default)
    {
        return !await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.NormalizedCertifiedEmail == normalizedCertifiedEmail
                    && tenant.Id != excludedTenantId.ToString(),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
