using NLog;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Abstractions.Views;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Contracts.Views;

namespace PSAAS.Application.Services.Views;

/// <summary>UC22: saved views of the list pages, personal to the administrator of the token.</summary>
public sealed class SavedViewService(ISavedViewRepository views, ICurrentActor actor, TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const int MaxNameLength = 40;
    public const int MaxViews = 20;
    public static readonly IReadOnlySet<string> Areas = new HashSet<string> { "tenants", "users" };

    public Task<IReadOnlyList<SavedView>> GetAsync(string area, CancellationToken cancellationToken = default) =>
        views.GetAsync(actor.Id, area, cancellationToken);

    public async Task<SavedViewResult> SaveAsync(SavedViewRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string>();
        var area = request.Area?.Trim() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;
        if (!Areas.Contains(area)) errors["area"] = TenantFieldValidationErrorCodes.InvalidValue;
        if (name.Length == 0) errors["name"] = TenantFieldValidationErrorCodes.Required;
        else if (name.Length > MaxNameLength) errors["name"] = TenantFieldValidationErrorCodes.MaxLengthExceeded;
        if (errors.Count == 0)
        {
            var existing = await views.GetAsync(actor.Id, area, cancellationToken);
            if (existing.Any(v => v.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))) errors["name"] = TenantFieldValidationErrorCodes.NotUnique;
            else if (existing.Count >= MaxViews) errors["name"] = TenantFieldValidationErrorCodes.LimitExceeded;
        }

        if (errors.Count > 0)
        {
            Logger.Info("Saved view rejected: {ValidationErrors}", errors);
            return new SavedViewResult(null, errors);
        }

        var view = new SavedView(Guid.NewGuid(), area, name, (request.Query ?? string.Empty).Trim().TrimStart('?'), clock.GetUtcNow());
        await views.AddAsync(actor.Id, view, cancellationToken);
        Logger.Info("Saved view {ViewId} for area {Area}", view.Id, area);
        return new SavedViewResult(view, errors);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => views.DeleteAsync(actor.Id, id, cancellationToken);
}
