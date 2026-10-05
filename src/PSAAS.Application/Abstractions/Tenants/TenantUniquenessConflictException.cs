namespace PSAAS.Application.Abstractions.Tenants;

/// <summary>
/// Thrown by a tenant write repository when an insert violates one of the
/// tenant unique indexes, meaning a concurrent insert won the uniqueness race.
/// </summary>
public sealed class TenantUniquenessConflictException : Exception
{
    public TenantUniquenessConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
