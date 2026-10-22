using PSAAS.Application.Contracts.Views;

namespace PSAAS.Application.Abstractions.Views;

/// <summary>UC22: personal saved views, by the id of the administrator in the token.</summary>
public interface ISavedViewRepository
{
    Task<IReadOnlyList<SavedView>> GetAsync(string ownerId, string area, CancellationToken cancellationToken = default);

    Task AddAsync(string ownerId, SavedView view, CancellationToken cancellationToken = default);

    /// <returns>False when the owner has no such view.</returns>
    Task<bool> DeleteAsync(string ownerId, Guid id, CancellationToken cancellationToken = default);
}
