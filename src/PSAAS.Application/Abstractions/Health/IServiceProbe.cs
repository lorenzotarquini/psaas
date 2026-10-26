namespace PSAAS.Application.Abstractions.Health;

/// <summary>UC26-R1.</summary>
public static class ServiceStatuses
{
    public const string Up = "Up";
    public const string Degraded = "Degraded";
    public const string Down = "Down";
    public const string NotConfigured = "NotConfigured";
}

/// <param name="Detail">What the administrator should know, e.g. "2 moduli in errore".</param>
public sealed record ProbeResult(string Status, string? Detail = null);

/// <summary>One connected system checked by UC26. A probe may throw: the caller turns it into Down.</summary>
public interface IServiceProbe
{
    /// <summary>Stable key, e.g. "keycloak-admin"; the frontend names it.</summary>
    string Key { get; }

    Task<ProbeResult> CheckAsync(CancellationToken cancellationToken);
}
