using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NLog;
using PSAAS.Application.Abstractions.Communications;
using PSAAS.Application.Abstractions.Health;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Communications;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Communications;

public sealed class CommunicationDbModel
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string[] Channels { get; set; } = [];

    /// <summary>The segment as JSON, to show who it was meant for.</summary>
    public string Segment { get; set; } = "{}";

    public DateOnly? VisibleUntil { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public string SentBy { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public int EmailsSent { get; set; }
    public int EmailsFailed { get; set; }
    public List<CommunicationRecipientDbModel> Recipients { get; set; } = [];
}

public sealed class CommunicationRecipientDbModel
{
    public Guid CommunicationId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? EmailStatus { get; set; }
    public string? Error { get; set; }
}

public sealed class CommunicationRepository(PsaasDbContext dbContext) : ICommunicationRepository
{
    public async Task AddAsync(Communication communication, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(communication);
        dbContext.Communications.Add(new CommunicationDbModel
        {
            Id = communication.Id,
            Subject = communication.Subject,
            Body = communication.Body,
            Channels = communication.Channels.ToArray(),
            Segment = JsonSerializer.Serialize(communication.Segment),
            VisibleUntil = communication.VisibleUntil,
            SentAt = communication.SentAt,
            SentBy = communication.SentBy,
            RecipientCount = communication.RecipientCount,
            EmailsSent = communication.EmailsSent,
            EmailsFailed = communication.EmailsFailed,
            Recipients = communication.Recipients.Select(r => new CommunicationRecipientDbModel
            {
                CommunicationId = communication.Id,
                TenantId = r.TenantId.ToString(),
                TenantName = r.TenantName,
                Email = r.Email,
                EmailStatus = r.EmailStatus,
                Error = r.Error,
            }).ToList(),
        });
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<Communication>> GetPageAsync(Guid? tenantId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Communications.AsNoTracking();
        if (tenantId is { } id)
        {
            var tenant = id.ToString();
            query = query.Where(c => c.Recipients.Any(r => r.TenantId == tenant));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query.OrderByDescending(c => c.SentAt).Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new PagedResult<Communication>(items.Select(c => ToCommunication(c, [])).ToArray(), total, page, pageSize);
    }

    public async Task<Communication?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await dbContext.Communications.AsNoTracking().Include(c => c.Recipients)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);
        return model is null
            ? null
            : ToCommunication(model, model.Recipients.OrderBy(r => r.TenantName)
                .Select(r => new CommunicationRecipient(Guid.Parse(r.TenantId), r.TenantName, r.Email, r.EmailStatus, r.Error)).ToArray());
    }

    private static Communication ToCommunication(CommunicationDbModel c, IReadOnlyList<CommunicationRecipient> recipients) => new(
        c.Id, c.Subject, c.Body, c.Channels, JsonSerializer.Deserialize<CommunicationSegment>(c.Segment) ?? new CommunicationSegment(),
        c.VisibleUntil, c.SentAt, c.SentBy, c.RecipientCount, c.EmailsSent, c.EmailsFailed, recipients);
}

/// <summary>
/// SMTP from Email:Smtp (Host, Port, User, Password, EnableSsl) and Email:From; or Email:PickupDirectory, where every
/// email is written as an .eml file (local development). Neither configured: nothing is sent.
/// </summary>
public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private string? From => configuration["Email:From"];
    private string? Host => configuration["Email:Smtp:Host"];
    private string? PickupDirectory => configuration["Email:PickupDirectory"];

    public bool IsConfigured => !string.IsNullOrWhiteSpace(From) && (!string.IsNullOrWhiteSpace(Host) || !string.IsNullOrWhiteSpace(PickupDirectory));

    /// <summary>For the services page: where emails go, without secrets.</summary>
    public string Destination => string.IsNullOrWhiteSpace(Host) ? $"pickup {PickupDirectory}" : $"smtp {Host}";

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!IsConfigured) throw new EmailDeliveryException("Email non configurata");
        using var client = new SmtpClient();
        if (!string.IsNullOrWhiteSpace(Host))
        {
            client.Host = Host;
            client.Port = int.TryParse(configuration["Email:Smtp:Port"], out var port) ? port : 587;
            client.EnableSsl = !bool.TryParse(configuration["Email:Smtp:EnableSsl"], out var ssl) || ssl;
            if (configuration["Email:Smtp:User"] is { Length: > 0 } user)
            {
                client.Credentials = new NetworkCredential(user, configuration["Email:Smtp:Password"]);
            }
        }
        else
        {
            Directory.CreateDirectory(PickupDirectory!);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = Path.GetFullPath(PickupDirectory!);
        }

        using var mail = new MailMessage(From!, message.To, message.Subject, message.Body);
        try
        {
            await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
            Logger.Info("Email \"{Subject}\" sent to {To}", message.Subject, message.To);
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or IOException)
        {
            throw new EmailDeliveryException(exception.Message, exception);
        }
    }
}

/// <summary>UC26: outgoing email; configuration only (connecting to SMTP on every check would be noisy).</summary>
public sealed class EmailProbe(IEmailSender email) : IServiceProbe
{
    public string Key => "email";

    public Task<ProbeResult> CheckAsync(CancellationToken cancellationToken) =>
        Task.FromResult(email.IsConfigured
            ? new ProbeResult(ServiceStatuses.Up, (email as SmtpEmailSender)?.Destination)
            : new ProbeResult(ServiceStatuses.NotConfigured));
}
