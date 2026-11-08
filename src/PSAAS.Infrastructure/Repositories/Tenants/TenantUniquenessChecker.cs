using Microsoft.EntityFrameworkCore;
using NLog;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Repositories.Tenants;

public sealed class TenantUniquenessChecker(PsaasDbContext dbContext) : ITenantUniquenessChecker
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // Only businesses need a unique name (UC3-R2): private persons may be homonyms.
    private const string BusinessKind = nameof(Domain.TenantKind.Business);

    public async Task<bool> IsCompanyNameUniqueAsync(
        string normalizedCompanyName,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                tenant => tenant.NormalizedCompanyName == normalizedCompanyName && tenant.Kind == BusinessKind,
                cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            Logger.Warn("Duplicate detected: IsCompanyName");
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
                tenant => tenant.NormalizedCompanyName == normalizedCompanyName && tenant.Kind == BusinessKind
                    && tenant.Id != excludedTenantId.ToString(),
                cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            Logger.Warn("Duplicate detected: IsCompanyName");
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
            Logger.Warn("Duplicate detected: IsCompleteVatNumber");
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
            Logger.Warn("Duplicate detected: IsCompleteVatNumber");
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
            Logger.Warn("Duplicate detected: IsCertifiedEmail");
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
            Logger.Warn("Duplicate detected: IsCertifiedEmail");
        }

        return !exists;
    }
}
