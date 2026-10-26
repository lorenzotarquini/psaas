using NLog;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Abstractions.Communications;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Application.Contracts.Communications;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Domain;

namespace PSAAS.Application.Services.Communications;

/// <summary>UC27 (EXP-11): announcements and emails to a segment of tenants.</summary>
public sealed class CommunicationService(
    ITenantListRepository tenants,
    ICommunicationRepository communications,
    IEmailSender email,
    IAuditLog audit,
    ICurrentActor actor,
    TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const int MaxSubjectLength = 150;
    public const int MaxBodyLength = 5000;
    public const int MaxRecipients = 1000;
    public const int MaxVisibleDays = 90;
    public const int DefaultVisibleDays = 30;

    /// <summary>UC27 step 3.</summary>
    public async Task<(CommunicationPreview? Preview, IReadOnlyDictionary<string, string> Errors)> PreviewAsync(
        CommunicationSegment? segment,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string>();
        var recipients = await ResolveAsync(segment, errors, cancellationToken);
        return errors.Count > 0
            ? (null, errors)
            : (new CommunicationPreview(recipients.Count, recipients.Take(5).Select(t => t.CompanyName).ToArray()), errors);
    }

    public async Task<CommunicationResult> SendAsync(CommunicationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string>();
        var subject = request.Subject?.Trim() ?? string.Empty;
        var body = request.Body?.Trim() ?? string.Empty;
        if (subject.Length == 0) errors["subject"] = TenantFieldValidationErrorCodes.Required;
        else if (subject.Length > MaxSubjectLength) errors["subject"] = TenantFieldValidationErrorCodes.MaxLengthExceeded;
        if (body.Length == 0) errors["body"] = TenantFieldValidationErrorCodes.Required;
        else if (body.Length > MaxBodyLength) errors["body"] = TenantFieldValidationErrorCodes.MaxLengthExceeded;
        var channels = (request.Channels ?? []).Distinct().ToArray();
        if (channels.Length == 0) errors["channels"] = TenantFieldValidationErrorCodes.Required;
        else if (channels.Any(c => !CommunicationChannels.All.Contains(c))) errors["channels"] = TenantFieldValidationErrorCodes.InvalidValue;
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var visibleUntil = channels.Contains(CommunicationChannels.InApp) ? request.VisibleUntil ?? today.AddDays(DefaultVisibleDays) : (DateOnly?)null;
        if (visibleUntil is { } until && (until < today || until > today.AddDays(MaxVisibleDays))) errors["visibleUntil"] = TenantFieldValidationErrorCodes.InvalidValue;
        var recipients = await ResolveAsync(request.Segment, errors, cancellationToken);
        if (errors.Count > 0)
        {
            Logger.Info("Communication rejected: {ValidationErrors}", errors);
            return new CommunicationResult(null, errors);
        }

        var sendEmail = channels.Contains(CommunicationChannels.Email);
        var delivered = new List<CommunicationRecipient>();
        foreach (var tenant in recipients)
        {
            var mailbox = tenant.CertifiedEmail.ToLowerInvariant();
            string? status = null, error = null;
            if (sendEmail && !email.IsConfigured)
            {
                (status, error) = (EmailStatuses.Skipped, "Email non configurata");
            }
            else if (sendEmail)
            {
                try
                {
                    await email.SendAsync(new EmailMessage(mailbox, subject, body), cancellationToken);
                    status = EmailStatuses.Sent;
                }
                catch (EmailDeliveryException exception)
                {
                    // UC27 4b: one refused mailbox does not stop the others.
                    Logger.Warn(exception, "Email of the communication to tenant {TenantId} refused", tenant.Id);
                    (status, error) = (EmailStatuses.Failed, exception.Message);
                }
            }

            delivered.Add(new CommunicationRecipient(tenant.Id, tenant.CompanyName, mailbox, status, error));
        }

        var communication = new Communication(
            Guid.NewGuid(), subject, body, channels, request.Segment ?? new CommunicationSegment(), visibleUntil, clock.GetUtcNow(), actor.Name,
            delivered.Count, delivered.Count(r => r.EmailStatus == EmailStatuses.Sent), delivered.Count(r => r.EmailStatus == EmailStatuses.Failed), delivered);
        await communications.AddAsync(communication, cancellationToken);
        foreach (var recipient in delivered)
        {
            await audit.RecordAsync(
                new AuditRecord(
                    AuditActions.CommunicationSent, AuditTargets.Communication, communication.Id.ToString(), subject, recipient.TenantId.ToString(),
                    new { channels, email = recipient.EmailStatus }),
                cancellationToken);
        }

        Logger.Info("Communication {CommunicationId} sent to {RecipientCount} tenants ({EmailsSent} emails)", communication.Id, delivered.Count, communication.EmailsSent);
        return new CommunicationResult(communication, errors);
    }

    // UC27-R1: never ceased tenants; an explicit selection wins over the filters.
    private async Task<IReadOnlyList<TenantListItem>> ResolveAsync(CommunicationSegment? segment, Dictionary<string, string> errors, CancellationToken cancellationToken)
    {
        segment ??= new CommunicationSegment();
        var statuses = (segment.Statuses ?? []).ToArray();
        if (statuses.Any(s => !Enum.TryParse<TenantStatus>(s, out _)))
        {
            errors["segment.statuses"] = TenantFieldValidationErrorCodes.InvalidValue;
            return [];
        }

        var notCeased = Enum.GetNames<TenantStatus>().Where(s => s != nameof(TenantStatus.Ceased)).ToArray();
        var query = segment.TenantIds is { Count: > 0 } ids
            ? new TenantListQuery(Statuses: notCeased, Ids: ids, PageSize: MaxRecipients + 1)
            : new TenantListQuery(
                Statuses: statuses.Length == 0 ? notCeased : statuses.Intersect(notCeased).DefaultIfEmpty("none").ToArray(),
                PlanIds: segment.PlanIds,
                ModuleId: segment.ModuleId,
                PageSize: MaxRecipients + 1);
        var page = await tenants.GetListAsync(query, clock.GetUtcNow(), cancellationToken);
        if (page.TotalCount == 0) errors["segment"] = TenantFieldValidationErrorCodes.InvalidValue;
        else if (page.TotalCount > MaxRecipients) errors["segment"] = TenantFieldValidationErrorCodes.LimitExceeded;
        return page.Items;
    }
}
