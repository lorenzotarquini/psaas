using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Communications;

namespace PSAAS.Application.Abstractions.Communications;

public interface ICommunicationRepository
{
    Task AddAsync(Communication communication, CancellationToken cancellationToken = default);

    /// <summary>Most recent first, without recipients; with <paramref name="tenantId"/>, those that reached that tenant.</summary>
    Task<PagedResult<Communication>> GetPageAsync(Guid? tenantId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Communication?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record EmailMessage(string To, string Subject, string Body);

/// <summary>Outgoing email (UC27, UC24-R3): SMTP in production, a fake in the tests.</summary>
public interface IEmailSender
{
    /// <summary>False until SMTP (or a pickup directory) is configured: nothing is sent (UC27 4a).</summary>
    bool IsConfigured { get; }

    /// <exception cref="EmailDeliveryException">The server refused or could not be reached.</exception>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed class EmailDeliveryException(string message, Exception? innerException = null) : Exception(message, innerException);
