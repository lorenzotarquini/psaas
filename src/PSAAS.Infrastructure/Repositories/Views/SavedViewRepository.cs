using Microsoft.EntityFrameworkCore;
using NLog;
using PSAAS.Application.Abstractions.Views;
using PSAAS.Application.Contracts.Views;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Repositories.Views;

public sealed class SavedViewDbModel
{
    public Guid Id { get; set; }

    /// <summary>The "sub" of the administrator (UC22-R1).</summary>
    public string OwnerId { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Query { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class SavedViewRepository(PsaasDbContext dbContext) : ISavedViewRepository
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<IReadOnlyList<SavedView>> GetAsync(string ownerId, string area, CancellationToken cancellationToken = default) =>
        await dbContext.SavedViews.AsNoTracking()
            .Where(v => v.OwnerId == ownerId && v.Area == area)
            .OrderBy(v => v.CreatedAt)
            .Select(v => new SavedView(v.Id, v.Area, v.Name, v.Query, v.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(string ownerId, SavedView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        dbContext.SavedViews.Add(new SavedViewDbModel
        {
            Id = view.Id,
            OwnerId = ownerId,
            Area = view.Area,
            Name = view.Name,
            Query = view.Query,
            CreatedAt = view.CreatedAt,
        });
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(string ownerId, Guid id, CancellationToken cancellationToken = default)
    {
        var view = await dbContext.SavedViews
            .SingleOrDefaultAsync(v => v.Id == id && v.OwnerId == ownerId, cancellationToken)
            .ConfigureAwait(false);
        if (view is null) return false;
        dbContext.SavedViews.Remove(view);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Logger.Info("Deleted saved view {ViewId}", id);
        return true;
    }
}
