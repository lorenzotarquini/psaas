namespace PSAAS.Infrastructure.Catalog;

public sealed class ModuleDbModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string Status { get; set; } = "Available";
    public string Version { get; set; } = string.Empty;
    public bool IsCore { get; set; }
    public string[] Requires { get; set; } = [];
    public string[] Permissions { get; set; } = [];
    public string? ProvisionUrl { get; set; }
    public string? DeprovisionUrl { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PlanDbModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string BillingPeriod { get; set; } = "Monthly";
    public int TrialDays { get; set; }
    public int? MaxUsers { get; set; }
    public int? MaxProductiveUnits { get; set; }
    public string[] ModuleIds { get; set; } = [];
    public bool Active { get; set; } = true;
}

/// <summary>One per tenant; plan values are copied in (grandfathering).</summary>
public sealed class SubscriptionDbModel
{
    public string TenantId { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public decimal Price { get; set; }
    public string BillingPeriod { get; set; } = "Monthly";
    public int? MaxUsers { get; set; }
    public int? MaxProductiveUnits { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? TrialEndsAt { get; set; }
    public DateTimeOffset RenewsAt { get; set; }
    public bool AutoRenew { get; set; } = true;
    public string[] AddOnModuleIds { get; set; } = [];
}

public sealed class TenantModuleDbModel
{
    public string TenantId { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public string Status { get; set; } = "Provisioning";
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
