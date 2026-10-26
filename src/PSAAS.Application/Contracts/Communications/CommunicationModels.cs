namespace PSAAS.Application.Contracts.Communications;

public static class CommunicationChannels
{
    /// <summary>Announcement shown inside the platform to the tenant's users.</summary>
    public const string InApp = "InApp";
    public const string Email = "Email";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { InApp, Email };
}

/// <summary>Outcome of the email to one tenant; null when the communication had no email channel.</summary>
public static class EmailStatuses
{
    public const string Sent = "Sent";
    public const string Failed = "Failed";

    /// <summary>Email not configured (UC27 4a).</summary>
    public const string Skipped = "Skipped";
}

/// <summary>UC27-R1: filters AND-ed, values of one filter OR-ed; an explicit selection ignores the filters.</summary>
public sealed record CommunicationSegment(
    IReadOnlyList<string>? Statuses = null,
    IReadOnlyList<Guid>? PlanIds = null,
    string? ModuleId = null,
    IReadOnlyList<Guid>? TenantIds = null);

/// <param name="VisibleUntil">Last day the in-app announcement shows (UC27-R3); null = 30 days.</param>
public sealed record CommunicationRequest(
    string? Subject,
    string? Body,
    IReadOnlyList<string>? Channels,
    CommunicationSegment? Segment,
    DateOnly? VisibleUntil = null);

/// <param name="Sample">The first tenants by name, to recognize the segment.</param>
public sealed record CommunicationPreview(int Count, IReadOnlyList<string> Sample);

public sealed record CommunicationRecipient(Guid TenantId, string TenantName, string Email, string? EmailStatus, string? Error);

/// <param name="Recipients">Only in the detail; empty in lists.</param>
public sealed record Communication(
    Guid Id,
    string Subject,
    string Body,
    IReadOnlyList<string> Channels,
    CommunicationSegment Segment,
    DateOnly? VisibleUntil,
    DateTimeOffset SentAt,
    string SentBy,
    int RecipientCount,
    int EmailsSent,
    int EmailsFailed,
    IReadOnlyList<CommunicationRecipient> Recipients);

public sealed record CommunicationResult(Communication? Communication, IReadOnlyDictionary<string, string> Errors);

/// <summary>UC28-R1.</summary>
public static class NotificationKinds
{
    public const string ModuleFailed = "module.failed";
    public const string InvoiceOverdue = "invoice.overdue";
    public const string TrialEnding = "trial.ending";
    public const string UserLimit = "users.limit";
    public const string DraftStalled = "draft.stalled";
}

public static class NotificationSeverities
{
    public const string Critical = "critical";
    public const string Warning = "warning";
    public const string Info = "info";
}

/// <summary>One item of the "Da gestire" queue (UC28): the frontend words it from the kind.</summary>
/// <param name="Subject">What the item is about: module name, invoice number…</param>
/// <param name="ObjectId">Id to link to (invoice id, module id), when there is one.</param>
public sealed record Notification(
    string Kind,
    string Severity,
    Guid TenantId,
    string TenantName,
    string? Subject,
    string? ObjectId,
    DateTimeOffset At,
    int? Value = null,
    int? Limit = null);
