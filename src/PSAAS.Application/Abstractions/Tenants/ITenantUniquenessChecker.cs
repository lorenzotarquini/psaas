namespace PSAAS.Application.Abstractions.Tenants;

public interface ITenantUniquenessChecker
{
    Task<bool> IsCompanyNameUniqueAsync(string normalizedCompanyName, CancellationToken cancellationToken = default);

    Task<bool> IsCompanyNameUniqueAsync(
        string normalizedCompanyName,
        Guid excludedTenantId,
        CancellationToken cancellationToken = default) =>
        IsCompanyNameUniqueAsync(normalizedCompanyName, cancellationToken);

    Task<bool> IsCompleteVatNumberUniqueAsync(string normalizedCompleteVatNumber, CancellationToken cancellationToken = default);

    Task<bool> IsCompleteVatNumberUniqueAsync(
        string normalizedCompleteVatNumber,
        Guid excludedTenantId,
        CancellationToken cancellationToken = default) =>
        IsCompleteVatNumberUniqueAsync(normalizedCompleteVatNumber, cancellationToken);

    Task<bool> IsCertifiedEmailUniqueAsync(string normalizedCertifiedEmail, CancellationToken cancellationToken = default);

    Task<bool> IsCertifiedEmailUniqueAsync(
        string normalizedCertifiedEmail,
        Guid excludedTenantId,
        CancellationToken cancellationToken = default) =>
        IsCertifiedEmailUniqueAsync(normalizedCertifiedEmail, cancellationToken);
}
