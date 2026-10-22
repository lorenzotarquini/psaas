using System.Text.RegularExpressions;
using NLog;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Application.Contracts.Catalog;
using PSAAS.Application.Contracts.Tenants;

namespace PSAAS.Application.Services.Catalog;

/// <summary>Outcome of a catalog write: errors by field, or the id of what was saved.</summary>
public sealed record CatalogSaveResult(string? Id, IReadOnlyDictionary<string, string> Errors)
{
    public bool Succeeded => Errors.Count == 0;
}

/// <summary>
/// UC15 (EXP-01): modules registered from their manifest; UC16 (EXP-02): plans as bundles of modules with limits.
/// </summary>
public sealed partial class CatalogService(ICatalogRepository catalog, IAuditLog audit)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const string CoreModuleId = "core";

    public async Task<CatalogSaveResult> RegisterModuleAsync(ModuleManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var errors = new Dictionary<string, string>();
        var id = manifest.Id?.Trim().ToLowerInvariant() ?? string.Empty;
        if (id.Length == 0) errors["id"] = TenantFieldValidationErrorCodes.Required;
        else if (!ModuleIdRegex().IsMatch(id)) errors["id"] = TenantFieldValidationErrorCodes.InvalidFormat;
        if (string.IsNullOrWhiteSpace(manifest.Name)) errors["name"] = TenantFieldValidationErrorCodes.Required;
        var version = manifest.Version?.Trim() ?? string.Empty;
        if (version.Length == 0) errors["version"] = TenantFieldValidationErrorCodes.Required;
        else if (!VersionRegex().IsMatch(version)) errors["version"] = TenantFieldValidationErrorCodes.InvalidFormat;
        var status = string.IsNullOrWhiteSpace(manifest.Status) ? ModuleStatuses.Available : manifest.Status.Trim();
        if (!ModuleStatuses.All.Contains(status)) errors["status"] = TenantFieldValidationErrorCodes.InvalidValue;
        foreach (var (url, field) in new[] { (manifest.ProvisionUrl, "provisionUrl"), (manifest.DeprovisionUrl, "deprovisionUrl") })
        {
            if (!string.IsNullOrWhiteSpace(url) && !(Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"))
            {
                errors[field] = TenantFieldValidationErrorCodes.InvalidFormat;
            }
        }

        var known = (await catalog.GetModulesAsync(cancellationToken)).Select(m => m.Id).ToHashSet();
        var requires = (manifest.Requires ?? []).Select(r => r.Trim().ToLowerInvariant()).ToArray();
        for (var i = 0; i < requires.Length; i++)
        {
            if (!known.Contains(requires[i]) || requires[i] == id)
            {
                errors[$"requires[{i}]"] = TenantFieldValidationErrorCodes.InvalidValue;
            }
        }

        if (errors.Count > 0)
        {
            Logger.Info("Module registration rejected: {ValidationErrors}", errors);
            return new CatalogSaveResult(null, errors);
        }

        var isNew = !known.Contains(id);
        await catalog.SaveModuleAsync(
            manifest with
            {
                Id = id,
                Name = manifest.Name!.Trim(),
                Version = version,
                Status = status,
                Requires = requires.Where(r => r != CoreModuleId).Distinct().ToArray(),
                Permissions = (manifest.Permissions ?? []).Select(p => p.Trim()).Where(p => p.Length > 0).Distinct().ToArray(),
            },
            cancellationToken);
        await audit.RecordAsync(
            new AuditRecord(isNew ? AuditActions.ModuleRegistered : AuditActions.ModuleUpdated, AuditTargets.Module, id, manifest.Name!.Trim(), null, new { version }),
            cancellationToken);
        Logger.Info("Module {ModuleId} {Change} at version {Version}", id, isNew ? "registered" : "updated", version);
        return new CatalogSaveResult(id, errors);
    }

    /// <summary>Beta, Available or Deprecated; core stays available.</summary>
    public async Task<CatalogSaveResult?> SetModuleStatusAsync(string moduleId, string? status, CancellationToken cancellationToken = default)
    {
        var module = await catalog.GetModuleAsync(moduleId, cancellationToken);
        if (module is null) return null;
        if (status is null || !ModuleStatuses.All.Contains(status) || (module.IsCore && status != ModuleStatuses.Available))
        {
            return new CatalogSaveResult(null, new Dictionary<string, string> { ["status"] = TenantFieldValidationErrorCodes.InvalidValue });
        }

        await catalog.SetModuleStatusAsync(moduleId, status, cancellationToken);
        await audit.RecordAsync(new AuditRecord(AuditActions.ModuleUpdated, AuditTargets.Module, moduleId, module.Name, null, new { status }), cancellationToken);
        return new CatalogSaveResult(moduleId, new Dictionary<string, string>());
    }

    public async Task<CatalogSaveResult?> SavePlanAsync(Guid? planId, PlanRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (planId is { } existing && await catalog.GetPlanAsync(existing, cancellationToken) is null)
        {
            return null;
        }

        var errors = new Dictionary<string, string>();
        var code = request.Code?.Trim().ToLowerInvariant() ?? string.Empty;
        if (code.Length == 0) errors["code"] = TenantFieldValidationErrorCodes.Required;
        else if (!PlanCodeRegex().IsMatch(code)) errors["code"] = TenantFieldValidationErrorCodes.InvalidFormat;
        else if (await catalog.IsPlanCodeTakenAsync(code, planId, cancellationToken)) errors["code"] = TenantFieldValidationErrorCodes.NotUnique;
        if (string.IsNullOrWhiteSpace(request.Name)) errors["name"] = TenantFieldValidationErrorCodes.Required;
        if (request.Price is null) errors["price"] = TenantFieldValidationErrorCodes.Required;
        else if (request.Price < 0) errors["price"] = TenantFieldValidationErrorCodes.InvalidValue;
        if (!BillingPeriods.All.Contains(request.BillingPeriod ?? string.Empty)) errors["billingPeriod"] = TenantFieldValidationErrorCodes.InvalidValue;
        if (request.TrialDays is < 0 or > 90) errors["trialDays"] = TenantFieldValidationErrorCodes.InvalidValue;
        if (request.MaxUsers is < 1) errors["maxUsers"] = TenantFieldValidationErrorCodes.InvalidValue;
        if (request.MaxProductiveUnits is < 1) errors["maxProductiveUnits"] = TenantFieldValidationErrorCodes.InvalidValue;
        var known = (await catalog.GetModulesAsync(cancellationToken)).Select(m => m.Id).ToHashSet();
        var modules = request.ModuleIds ?? [];
        for (var i = 0; i < modules.Count; i++)
        {
            if (!known.Contains(modules[i])) errors[$"moduleIds[{i}]"] = TenantFieldValidationErrorCodes.InvalidValue;
        }

        if (errors.Count > 0)
        {
            Logger.Info("Plan save rejected: {ValidationErrors}", errors);
            return new CatalogSaveResult(null, errors);
        }

        var id = await catalog.SavePlanAsync(
            planId,
            request with
            {
                Code = code,
                Name = request.Name!.Trim(),
                TrialDays = request.TrialDays ?? 0,
                // Core is in every plan.
                ModuleIds = modules.Append(CoreModuleId).Distinct().ToArray(),
            },
            cancellationToken);
        await audit.RecordAsync(
            new AuditRecord(planId is null ? AuditActions.PlanCreated : AuditActions.PlanUpdated, AuditTargets.Plan, id.ToString(), request.Name.Trim(), null, new { price = request.Price, request.BillingPeriod }),
            cancellationToken);
        return new CatalogSaveResult(id.ToString(), errors);
    }

    [GeneratedRegex("^[a-z][a-z0-9-]{1,39}$")]
    private static partial Regex ModuleIdRegex();

    [GeneratedRegex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?$")]
    private static partial Regex VersionRegex();

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,29}$")]
    private static partial Regex PlanCodeRegex();
}
