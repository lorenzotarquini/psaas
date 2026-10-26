using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Abstractions.Billing;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Abstractions.Communications;
using PSAAS.Application.Abstractions.Geography;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Application.Contracts.Billing;
using PSAAS.Application.Contracts.Catalog;
using PSAAS.Application.Contracts.Profiles;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Contracts.Users;
using PSAAS.Domain;

namespace PSAAS.Application.Tests;

/// <summary>Normalized values already taken, each by the tenant that owns it.</summary>
public sealed class FakeUniquenessChecker : ITenantUniquenessChecker
{
    public Dictionary<string, Guid> Taken { get; } = [];

    public Task<bool> IsCompanyNameUniqueAsync(string normalizedCompanyName, CancellationToken cancellationToken = default) =>
        Task.FromResult(!Taken.ContainsKey(normalizedCompanyName));

    public Task<bool> IsCompanyNameUniqueAsync(string normalizedCompanyName, Guid excludedTenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(IsFreeFor(normalizedCompanyName, excludedTenantId));

    public Task<bool> IsCompleteVatNumberUniqueAsync(string normalizedCompleteVatNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(!Taken.ContainsKey(normalizedCompleteVatNumber));

    public Task<bool> IsCompleteVatNumberUniqueAsync(string normalizedCompleteVatNumber, Guid excludedTenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(IsFreeFor(normalizedCompleteVatNumber, excludedTenantId));

    public Task<bool> IsCertifiedEmailUniqueAsync(string normalizedCertifiedEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult(!Taken.ContainsKey(normalizedCertifiedEmail));

    public Task<bool> IsCertifiedEmailUniqueAsync(string normalizedCertifiedEmail, Guid excludedTenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(IsFreeFor(normalizedCertifiedEmail, excludedTenantId));

    private bool IsFreeFor(string value, Guid tenantId) => !Taken.TryGetValue(value, out var owner) || owner == tenantId;
}

public sealed class FakeCityList : IItalianCityProvider
{
    public List<ItalianCity> Cities { get; } = [new("Roma", "RM", ["00118", "00100"])];

    public bool Unavailable { get; set; }

    public Task<IReadOnlyList<ItalianCity>?> SearchAsync(string term, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ItalianCity>?>(Unavailable
            ? null
            : Cities.Where(city => city.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToArray());
}

/// <summary>Records what the use case asked to persist.</summary>
public sealed class FakeTenantWriteRepository : ITenantWriteRepository
{
    public Tenant? Saved { get; private set; }

    public IReadOnlyCollection<TenantProfileCreateModel> SavedProfiles { get; private set; } = [];

    public IReadOnlyCollection<TenantProductiveUnitCreateModel> SavedProductiveUnits { get; private set; } = [];

    /// <summary>Runs before saving: lets a test simulate a concurrent insert winning the race.</summary>
    public Action? BeforeSave { get; set; }

    public Task AddWithProfilesAndProductiveUnitsAsync(
        Tenant tenant,
        IReadOnlyCollection<TenantProfileCreateModel> profiles,
        IReadOnlyCollection<TenantProductiveUnitCreateModel> productiveUnits,
        CancellationToken cancellationToken = default)
    {
        BeforeSave?.Invoke();
        (Saved, SavedProfiles, SavedProductiveUnits) = (tenant, profiles, productiveUnits);
        return Task.CompletedTask;
    }

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task AddWithProductiveUnitsAsync(Tenant tenant, IReadOnlyCollection<TenantProductiveUnitCreateModel> productiveUnits, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        BeforeSave?.Invoke();
        Saved = tenant;
        return Task.FromResult(true);
    }

    public TenantLifecycle? SavedLifecycle { get; private set; }

    public bool Deleted { get; private set; }

    public Task<bool> UpdateLifecycleAsync(Guid tenantId, TenantLifecycle lifecycle, CancellationToken cancellationToken = default)
    {
        SavedLifecycle = lifecycle;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        Deleted = true;
        return Task.FromResult(true);
    }

    public IReadOnlyCollection<TenantProfileEditModel> EditedProfiles { get; private set; } = [];

    public IReadOnlyCollection<TenantProductiveUnitEditModel> EditedProductiveUnits { get; private set; } = [];

    public Task<bool> UpdateWithStructureAsync(
        Tenant tenant,
        IReadOnlyCollection<TenantProfileEditModel> profiles,
        IReadOnlyCollection<TenantProductiveUnitEditModel> productiveUnits,
        CancellationToken cancellationToken = default)
    {
        (Saved, EditedProfiles, EditedProductiveUnits) = (tenant, profiles, productiveUnits);
        return Task.FromResult(true);
    }
}

/// <summary>Profiles that exist, each with its tenant.</summary>
public sealed class FakeProfileReadRepository : IProfileReadRepository
{
    public List<UserProfileReadModel> Profiles { get; } = [];

    public List<ProfileReadModel> All { get; } = [];

    public Task<IReadOnlyList<ProfileReadModel>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProfileReadModel>>(All);

    public Task<IReadOnlyList<UserProfileReadModel>> GetWithTenantAsync(
        IReadOnlyCollection<string> profileIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UserProfileReadModel>>(Profiles.Where(profile => profileIds.Contains(profile.Id)).ToArray());
}

/// <summary>Records what the use case asked the identity provider to create.</summary>
public sealed class FakeUserDirectory : IUserDirectory
{
    public List<DirectoryUser> Users { get; } = [];

    public NewDirectoryUser? Created { get; private set; }

    public bool EmailTaken { get; set; }

    public bool Unavailable { get; set; }

    public Task<DirectoryUser> CreateAsync(NewDirectoryUser user, CancellationToken cancellationToken = default)
    {
        if (Unavailable) throw new UserDirectoryUnavailableException("down");
        if (EmailTaken) throw new UserEmailConflictException("taken");
        Created = user;
        var created = new DirectoryUser("kc-1", user.FirstName, user.LastName, user.Email, user.BirthDate, user.ProfileIds, PasswordPending: true, CreatedBy: user.CreatedBy, InvitesSent: 1);
        Users.Add(created);
        return Task.FromResult(created);
    }

    public Task<IReadOnlyList<DirectoryUser>> SearchAsync(string? search, CancellationToken cancellationToken = default) =>
        Unavailable ? throw new UserDirectoryUnavailableException("down") : Task.FromResult<IReadOnlyList<DirectoryUser>>(Users);

    public List<string> RemovedProfiles { get; } = [];

    public Task<IReadOnlyDictionary<string, int>> CountByProfilesAsync(
        IReadOnlyCollection<string> profileIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<string, int>>(profileIds.ToDictionary(id => id, id => Users.Count(u => u.ProfileIds.Contains(id))));

    public Task<int> CountUsersWithAnyAsync(IReadOnlyCollection<string> profileIds, CancellationToken cancellationToken = default)
    {
        ThrowIfDown();
        return Task.FromResult(Users.Count(u => u.ProfileIds.Intersect(profileIds).Any()));
    }

    public Task RemoveProfilesAsync(IReadOnlyCollection<string> profileIds, CancellationToken cancellationToken = default)
    {
        if (Unavailable) throw new UserDirectoryUnavailableException("down");
        RemovedProfiles.AddRange(profileIds);
        return Task.CompletedTask;
    }

    /// <summary>Emails asked of Keycloak by UC19: (user id, "invite" or "reset").</summary>
    public List<(string UserId, string Kind)> ActionEmails { get; } = [];

    public Dictionary<string, DateTimeOffset> LastLogins { get; } = [];

    public Task<DirectoryUser?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ThrowIfDown();
        return Task.FromResult(Users.FirstOrDefault(user => user.Id == id));
    }

    public Task<bool> UpdateAsync(string id, DirectoryUserUpdate update, CancellationToken cancellationToken = default)
    {
        ThrowIfDown();
        var index = Users.FindIndex(user => user.Id == id);
        if (index < 0) return Task.FromResult(false);
        Users[index] = Users[index] with
        {
            FirstName = update.FirstName,
            LastName = update.LastName,
            BirthDate = update.BirthDate,
            ProfileIds = update.ProfileIds,
            MfaRequired = update.MfaRequired,
        };
        return Task.FromResult(true);
    }

    public Task SendInviteAsync(string id, DateTimeOffset sentAt, CancellationToken cancellationToken = default)
    {
        ThrowIfDown();
        var index = Users.FindIndex(user => user.Id == id);
        Users[index] = Users[index] with { InvitesSent = Users[index].InvitesSent + 1, LastInviteAt = sentAt };
        ActionEmails.Add((id, "invite"));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string id, CancellationToken cancellationToken = default)
    {
        ThrowIfDown();
        ActionEmails.Add((id, "reset"));
        return Task.CompletedTask;
    }

    public Task SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default)
    {
        ThrowIfDown();
        var index = Users.FindIndex(user => user.Id == id);
        Users[index] = Users[index] with { Enabled = enabled };
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, DateTimeOffset>> GetLastLoginsAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<string, DateTimeOffset>>(LastLogins.Where(l => userIds.Contains(l.Key)).ToDictionary());

    private void ThrowIfDown()
    {
        if (Unavailable) throw new UserDirectoryUnavailableException("down");
    }
}

public sealed class FakeCurrentActor : ICurrentActor
{
    public string Id => "kc-admin";

    public string Name => "Marta Russo";
}

/// <summary>One tenant with its organization, as the read side returns it.</summary>
public sealed class FakeTenantReadRepository : ITenantReadRepository
{
    public Tenant? Tenant { get; set; }

    public TenantStructureReadModel Structure { get; set; } = new([], []);

    public Task<PagedResult<Tenant>> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tenant?.Id == tenantId ? Tenant : null);

    public Task<TenantReference?> FindByUniqueValueAsync(string field, string normalizedValue, Guid? excludedTenantId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<TenantStructureReadModel> GetProfilesAndProductiveUnitsAsync(string tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Structure);
}

/// <summary>Records what the use cases asked to audit.</summary>
public sealed class FakeAuditLog : IAuditLog
{
    public List<AuditRecord> Records { get; } = [];

    public Task RecordAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        Records.Add(record);
        return Task.CompletedTask;
    }
}

/// <summary>Subscriptions in memory, with the module statuses the use case set.</summary>
public sealed class FakeSubscriptionRepository : ISubscriptionRepository
{
    public Dictionary<Guid, SubscriptionReadModel> Subscriptions { get; } = [];

    public Dictionary<(Guid, string), (string Status, string? Error, int Attempts)> Modules { get; } = [];

    public Task<SubscriptionReadModel?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Subscriptions.TryGetValue(tenantId, out var s)
            ? s with
            {
                Modules = Modules.Where(m => m.Key.Item1 == tenantId)
                    .Select(m => new TenantModuleReadModel(m.Key.Item2, m.Key.Item2, null, "1.0.0", m.Value.Status, m.Value.Attempts, m.Value.Error, false, DateTimeOffset.UtcNow))
                    .ToArray(),
            }
            : null);

    public Task<IReadOnlyList<SubscriptionReadModel>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SubscriptionReadModel>>(Subscriptions.Values.ToArray());

    public Task SaveAsync(SubscriptionReadModel subscription, IReadOnlyCollection<string> moduleIds, CancellationToken cancellationToken = default)
    {
        Subscriptions[subscription.TenantId] = subscription;
        foreach (var key in Modules.Keys.Where(k => k.Item1 == subscription.TenantId && !moduleIds.Contains(k.Item2)).ToArray()) Modules.Remove(key);
        foreach (var id in moduleIds) Modules.TryAdd((subscription.TenantId, id), (ProvisioningStatuses.Provisioning, null, 0));
        return Task.CompletedTask;
    }

    public Task SetModuleStatusAsync(Guid tenantId, string moduleId, string status, string? error, CancellationToken cancellationToken = default)
    {
        var attempts = Modules.TryGetValue((tenantId, moduleId), out var current) ? current.Attempts : 0;
        Modules[(tenantId, moduleId)] = (status, error, status == ProvisioningStatuses.Provisioning ? attempts + 1 : attempts);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<(Guid TenantId, string ModuleId)>> GetFailedAsync(int maxAttempts, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<(Guid, string)>>(Modules.Where(m => m.Value.Status == ProvisioningStatuses.Failed && m.Value.Attempts < maxAttempts).Select(m => m.Key).ToArray());
}

public sealed class FakeCatalogRepository : ICatalogRepository
{
    public List<ModuleReadModel> Modules { get; } = [Module("core", isCore: true)];

    public List<PlanReadModel> Plans { get; } = [];

    public static ModuleReadModel Module(string id, bool isCore = false, string[]? requires = null, string? hook = null) =>
        new(id, id, null, null, ModuleStatuses.Available, "1.0.0", isCore, requires ?? [], [], hook, DateTimeOffset.UtcNow, 0);

    public static PlanReadModel Plan(string code, int? maxUsers = null, int trialDays = 0, params string[] modules) =>
        new(Guid.NewGuid(), code, code, null, 99m, BillingPeriods.Monthly, trialDays, maxUsers, null, ["core", .. modules], true, 0);

    public Task<IReadOnlyList<ModuleReadModel>> GetModulesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ModuleReadModel>>(Modules);

    public Task<ModuleReadModel?> GetModuleAsync(string moduleId, CancellationToken cancellationToken = default) => Task.FromResult(Modules.FirstOrDefault(m => m.Id == moduleId));

    public Task SaveModuleAsync(ModuleManifest manifest, CancellationToken cancellationToken = default)
    {
        Modules.RemoveAll(m => m.Id == manifest.Id);
        Modules.Add(Module(manifest.Id!, requires: manifest.Requires?.ToArray()) with { Name = manifest.Name!, Version = manifest.Version!, Status = manifest.Status! });
        return Task.CompletedTask;
    }

    public Task<bool> SetModuleStatusAsync(string moduleId, string status, CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<IReadOnlyList<PlanReadModel>> GetPlansAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PlanReadModel>>(Plans);

    public Task<PlanReadModel?> GetPlanAsync(Guid planId, CancellationToken cancellationToken = default) => Task.FromResult(Plans.FirstOrDefault(p => p.Id == planId));

    public Task<bool> IsPlanCodeTakenAsync(string code, Guid? exceptPlanId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Plans.Any(p => p.Code == code && p.Id != exceptPlanId));

    public Task<Guid> SavePlanAsync(Guid? planId, PlanRequest plan, CancellationToken cancellationToken = default)
    {
        var id = planId ?? Guid.NewGuid();
        Plans.RemoveAll(p => p.Id == id);
        Plans.Add(new(id, plan.Code!, plan.Name!, plan.Description, plan.Price!.Value, plan.BillingPeriod!, plan.TrialDays ?? 0, plan.MaxUsers, plan.MaxProductiveUnits, plan.ModuleIds ?? [], plan.Active, 0));
        return Task.FromResult(id);
    }
}

/// <summary>Provisioning hooks: modules listed in Failing answer with an error.</summary>
public sealed class FakeModuleProvisioner : IModuleProvisioner
{
    public HashSet<string> Failing { get; } = [];

    public List<string> Provisioned { get; } = [];

    public Task<ProvisioningOutcome> ProvisionAsync(ModuleReadModel module, Guid tenantId, CancellationToken cancellationToken = default)
    {
        Provisioned.Add(module.Id);
        return Task.FromResult(Failing.Contains(module.Id) ? new ProvisioningOutcome(false, "timeout") : new ProvisioningOutcome(true, null));
    }
}

/// <summary>Invoices in memory, numbered per year like the real repository.</summary>
public sealed class FakeInvoiceRepository : IInvoiceRepository
{
    public List<Invoice> Invoices { get; } = [];

    public Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        var numbered = invoice with { Number = $"{invoice.IssuedAt.Year}/{Invoices.Count(i => i.IssuedAt.Year == invoice.IssuedAt.Year) + 1:0000}" };
        Invoices.Add(numbered);
        return Task.FromResult(numbered);
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        Invoices[Invoices.FindIndex(i => i.Id == invoice.Id)] = invoice;
        return Task.CompletedTask;
    }

    public Task<Invoice?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Invoices.FirstOrDefault(i => i.Id == id));

    public Task<PagedResult<Invoice>> GetPageAsync(InvoiceQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<Invoice>(Invoices, Invoices.Count, 1, Invoices.Count));

    public Task<IReadOnlyList<Invoice>> GetOpenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Invoice>>(Invoices.Where(i => i.Kind == InvoiceKinds.Invoice && InvoiceStatuses.IsOpen(i.Status)).ToArray());

    public Task<bool> ExistsForPeriodAsync(Guid tenantId, DateTimeOffset periodFrom, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoices.Any(i => i.TenantId == tenantId && i.PeriodFrom == periodFrom && i.Kind == InvoiceKinds.Invoice && i.Status != InvoiceStatuses.Cancelled));

    public Task<BillingSummary> GetSummaryAsync(DateTimeOffset monthStart, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

public sealed class FakeInvoiceTransmitter : IInvoiceTransmitter
{
    public List<string> Transmitted { get; } = [];

    public Task TransmitAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        Transmitted.Add(invoice.Number);
        return Task.CompletedTask;
    }
}

/// <summary>An outbox: what would have been emailed; addresses in Rejecting are refused.</summary>
public sealed class FakeEmailSender : IEmailSender
{
    public bool IsConfigured { get; set; } = true;

    public List<EmailMessage> Sent { get; } = [];

    public HashSet<string> Rejecting { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (Rejecting.Contains(message.To)) throw new EmailDeliveryException($"550 mailbox {message.To} unavailable");
        Sent.Add(message);
        return Task.CompletedTask;
    }
}
