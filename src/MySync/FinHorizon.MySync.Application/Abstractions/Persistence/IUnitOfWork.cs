namespace FinHorizon.MySync.Application.Abstractions.Persistence;

/// <summary>Commits everything a use case changed, all or nothing.</summary>
public interface IUnitOfWork
{
    /// <exception cref="DuplicateNameException">Another request stored the same unique name first.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// A unique name was taken between the check of the use case and the commit: the database index is the last guard.
/// </summary>
public sealed class DuplicateNameException : Exception
{
    public DuplicateNameException()
    {
    }

    public DuplicateNameException(string message)
        : base(message)
    {
    }

    public DuplicateNameException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
