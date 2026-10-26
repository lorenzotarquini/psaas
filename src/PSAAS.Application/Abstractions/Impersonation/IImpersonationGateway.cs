namespace PSAAS.Application.Abstractions.Impersonation;

/// <summary>
/// UC31: opens a session of a platform user for an administrator (Keycloak token exchange in production, a fake in
/// the tests). Not configured: impersonation is unavailable and says so.
/// </summary>
public interface IImpersonationGateway
{
    bool IsConfigured { get; }

    /// <returns>The platform address carrying the session in its fragment (UC31-R2).</returns>
    /// <exception cref="ImpersonationUnavailableException">The identity provider refused or could not be reached.</exception>
    Task<string> CreateSessionUrlAsync(string userId, TimeSpan duration, CancellationToken cancellationToken = default);
}

public sealed class ImpersonationUnavailableException(string message, Exception? innerException = null) : Exception(message, innerException);
