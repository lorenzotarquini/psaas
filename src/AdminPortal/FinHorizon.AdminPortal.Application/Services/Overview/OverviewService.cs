using FinHorizon.Modules.Sdk;
using NLog;
using FinHorizon.AdminPortal.Application.Abstractions.Audit;
using FinHorizon.AdminPortal.Application.Abstractions.Catalog;
using FinHorizon.AdminPortal.Application.Abstractions.Impersonation;
using FinHorizon.AdminPortal.Application.Abstractions.Tenants;
using FinHorizon.AdminPortal.Application.Abstractions.Users;
using FinHorizon.AdminPortal.Application.Contracts.Audit;
using FinHorizon.AdminPortal.Application.Contracts.Catalog;
using FinHorizon.AdminPortal.Application.Contracts.Overview;
using FinHorizon.AdminPortal.Application.Contracts.Tenants;
using FinHorizon.AdminPortal.Application.Contracts.Users;
using FinHorizon.AdminPortal.Application.Services.Users;
using FinHorizon.AdminPortal.Domain;

namespace FinHorizon.AdminPortal.Application.Services.Overview;

/// <summary>UC29 (mock 1b): the KPIs of the Panoramica.</summary>
public sealed class OverviewService(ITenantListRepository tenants, ICatalogRepository catalog, IUserDirectory users, TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const int TrialEndingDays = 7;

    public async Task<OverviewKpis> GetKpisAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        // shortcut: lists at most 10000 tenants to count them; a dedicated count query when tenants grow beyond.
        var active = (await tenants.GetListAsync(new TenantListQuery(Statuses: [nameof(TenantStatus.Active)], PageSize: 10000), now, cancellationToken)).Items;
        var trials = (await tenants.GetListAsync(new TenantListQuery(Statuses: [nameof(TenantStatus.Trial)], PageSize: 10000), now, cancellationToken)).Items;
        var modules = await catalog.GetModulesAsync(cancellationToken);

        int? userCount = null, usersThisMonth = null;
        try
        {
            var all = await users.SearchAsync(null, cancellationToken);
            (userCount, usersThisMonth) = (all.Count, all.Count(u => u.CreatedAt >= monthStart));
        }
        catch (UserDirectoryUnavailableException exception)
        {
            Logger.Warn(exception, "Overview without users: Keycloak unreachable");
        }

        return new OverviewKpis(
            active.Count, active.Count(t => t.CreatedAt >= monthStart),
            trials.Count, trials.Count(t => t.RenewsAt is { } end && end <= now.AddDays(TrialEndingDays)),
            userCount, usersThisMonth,
            modules.Count, modules.Count(m => m.Status == ModuleStatuses.Beta));
    }
}

/// <summary>UC30: clients and users in one search, at most 5 each.</summary>
public sealed class GlobalSearchService(ITenantListRepository tenants, UserSearchService users, TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const int MinLength = 2;
    public const int MaxResults = 5;

    /// <returns>Null when the text is too short (UC30 1a).</returns>
    public async Task<SearchResult?> SearchAsync(string? text, CancellationToken cancellationToken = default)
    {
        var term = text?.Trim() ?? string.Empty;
        if (term.Length < MinLength) return null;
        var found = await tenants.GetListAsync(new TenantListQuery(Search: term, PageSize: MaxResults), clock.GetUtcNow(), cancellationToken);
        var tenantHits = found.Items.Select(t => new SearchTenant(t.Id, t.CompanyName, t.CompleteVatNumber, t.CertifiedEmail, t.Status)).ToArray();
        try
        {
            var userHits = await users.SearchAsync(new UserQuery(term, null, null, null, 1, MaxResults), cancellationToken);
            return new SearchResult(tenantHits, userHits.Items, false);
        }
        catch (UserDirectoryUnavailableException exception)
        {
            Logger.Warn(exception, "Global search without users: Keycloak unreachable");
            return new SearchResult(tenantHits, [], true);
        }
    }
}

/// <summary>UC31 (EXP-12): a time-limited, reasoned and audited session as a platform user.</summary>
public sealed class ImpersonationService(UserManagementService users, IImpersonationGateway gateway, UserRules rules, TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const int MinReasonLength = 10;
    public const int DefaultMinutes = 30;

    public bool IsConfigured => gateway.IsConfigured;

    /// <exception cref="ImpersonationUnavailableException">Keycloak refused the token exchange.</exception>
    public async Task<ImpersonationResult> StartAsync(string userId, ImpersonationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string>();
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length == 0) errors["reason"] = TenantFieldValidationErrorCodes.Required;
        else if (reason.Length < MinReasonLength) errors["reason"] = TenantFieldValidationErrorCodes.InvalidValue;
        var minutes = request.Minutes ?? DefaultMinutes;
        if (minutes is < 5 or > 60) errors["minutes"] = TenantFieldValidationErrorCodes.InvalidValue;
        if (errors.Count > 0) return new ImpersonationResult(null, true, false, errors);

        var user = await users.GetAsync(userId, cancellationToken);
        if (user is null) return new ImpersonationResult(null, false, false, errors);
        // UC31 1a, 1b: nothing to open when not configured, nobody to be when the user cannot log in.
        if (!gateway.IsConfigured || user.Status != UserStatuses.Active) return new ImpersonationResult(null, true, true, errors);

        var duration = TimeSpan.FromMinutes(minutes);
        var url = await gateway.CreateSessionUrlAsync(userId, duration, cancellationToken);
        await rules.RecordAsync(AuditActions.UserImpersonated, user, _ => new { email = user.Email, reason, minutes }, cancellationToken);
        Logger.Warn("Administrator impersonates user {UserId} for {Minutes} minutes: {Reason}", userId, minutes, reason);
        return new ImpersonationResult(new ImpersonationTicket(url, clock.GetUtcNow().Add(duration)), true, false, errors);
    }
}
