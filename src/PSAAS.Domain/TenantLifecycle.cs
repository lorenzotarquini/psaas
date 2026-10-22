namespace PSAAS.Domain;

/// <summary>
/// EXP-04: where a tenant is in its life. Onboarding is a draft being set up; Trial and Active can use the
/// platform; Suspended keeps the data but blocks access; Cancelling is a notice period; Ceased is the soft delete
/// (UC3-R8: a ceased tenant still holds its company name, VAT number and PEC).
/// </summary>
public enum TenantStatus
{
    Onboarding,
    Trial,
    Active,
    Suspended,
    Cancelling,
    Ceased,
}

public sealed record TenantLifecycle(TenantStatus Status, string? Reason, DateTimeOffset ChangedAt, DateTimeOffset CreatedAt)
{
    private static readonly Dictionary<TenantStatus, TenantStatus[]> Allowed = new()
    {
        [TenantStatus.Onboarding] = [TenantStatus.Trial, TenantStatus.Active],
        [TenantStatus.Trial] = [TenantStatus.Active, TenantStatus.Suspended, TenantStatus.Ceased],
        [TenantStatus.Active] = [TenantStatus.Suspended, TenantStatus.Cancelling],
        [TenantStatus.Suspended] = [TenantStatus.Active, TenantStatus.Ceased],
        [TenantStatus.Cancelling] = [TenantStatus.Active, TenantStatus.Ceased],
        [TenantStatus.Ceased] = [],
    };

    public static TenantLifecycle New(TenantStatus status, DateTimeOffset now) => new(status, null, now, now);

    public bool CanChangeTo(TenantStatus next) => Allowed[Status].Contains(next);

    /// <summary>Suspending, giving notice and ceasing must say why (it ends up in the history).</summary>
    public static bool RequiresReason(TenantStatus next) =>
        next is TenantStatus.Suspended or TenantStatus.Cancelling or TenantStatus.Ceased;

    public TenantLifecycle ChangeTo(TenantStatus next, string? reason, DateTimeOffset now)
    {
        if (!CanChangeTo(next))
        {
            throw new InvalidOperationException($"A tenant cannot go from {Status} to {next}.");
        }

        if (RequiresReason(next) && string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException($"Going to {next} requires a reason.", nameof(reason));
        }

        return this with { Status = next, Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(), ChangedAt = now };
    }
}

/// <summary>Commercial data of a tenant (mock 1d/1e): electronic invoicing code and account manager.</summary>
/// <param name="SdiCode">Codice destinatario SDI: 7 characters (6 for public administrations).</param>
public sealed record TenantCommercialInfo(string? SdiCode, string? AccountManager)
{
    public static readonly TenantCommercialInfo Empty = new(null, null);
}
