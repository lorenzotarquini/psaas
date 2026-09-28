namespace PSAAS.Application.Abstractions.Tenants;

public interface ITenantUniquenessChecker
{
    Task<bool> IsCompanyNameUniqueAsync(string normalizedCompanyName, CancellationToken cancellationToken = default);

    Task<bool> IsCompleteVatNumberUniqueAsync(string normalizedCompleteVatNumber, CancellationToken cancellationToken = default);

    Task<bool> IsCertifiedEmailUniqueAsync(string normalizedCertifiedEmail, CancellationToken cancellationToken = default);
}
