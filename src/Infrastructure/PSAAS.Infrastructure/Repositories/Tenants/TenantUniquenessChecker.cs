using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Repositories.Tenants;

public sealed class TenantUniquenessChecker(
    PsaasDbContext dbContext,
    ILogger<TenantUniquenessChecker>? logger = null) : ITenantUniquenessChecker
{
    private readonly ILogger<TenantUniquenessChecker> _logger = logger ?? NullLogger<TenantUniquenessChecker>.Instance;

    public async Task<bool> IsCompanyNameUniqueAsync(
        string normalizedCompanyName,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.NormalizedCompanyName == normalizedCompanyName,
                cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            _logger.LogWarning("Duplicate detected: IsCompanyName");
        }

        return !exists;
    }

    public async Task<bool> IsCompanyNameUniqueAsync(
        string normalizedCompanyName,
        Guid excludedTenantId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.NormalizedCompanyName == normalizedCompanyName
                    && tenant.Id != excludedTenantId.ToString(),
                cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            _logger.LogWarning("Duplicate detected: IsCompanyName");
        }

        return !exists;
    }

    public async Task<bool> IsCompleteVatNumberUniqueAsync(
        string normalizedCompleteVatNumber,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.CompleteVatNumber == normalizedCompleteVatNumber,
                cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            _logger.LogWarning("Duplicate detected: IsCompleteVatNumber");
        }

        return !exists;
    }

    public async Task<bool> IsCompleteVatNumberUniqueAsync(
        string normalizedCompleteVatNumber,
        Guid excludedTenantId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.CompleteVatNumber == normalizedCompleteVatNumber
                    && tenant.Id != excludedTenantId.ToString(),
                cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            _logger.LogWarning("Duplicate detected: IsCompleteVatNumber");
        }

        return !exists;
    }

    public async Task<bool> IsCertifiedEmailUniqueAsync(
        string normalizedCertifiedEmail,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.NormalizedCertifiedEmail == normalizedCertifiedEmail,
                cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            _logger.LogWarning("Duplicate detected: IsCertifiedEmail");
        }

        return !exists;
    }

    public async Task<bool> IsCertifiedEmailUniqueAsync(
        string normalizedCertifiedEmail,
        Guid excludedTenantId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.NormalizedCertifiedEmail == normalizedCertifiedEmail
                    && tenant.Id != excludedTenantId.ToString(),
                cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            _logger.LogWarning("Duplicate detected: IsCertifiedEmail");
        }

        return !exists;
    }
}
