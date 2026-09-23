namespace PSAAS.Infrastructure.Data.DbModels;

public sealed class ProductiveUnitDbModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string TenantId { get; set; } = string.Empty;

    public TenantDbModel? Tenant { get; set; }

    public string ProfileId { get; set; } = string.Empty;

    public ProfileDbModel? Profile { get; set; }
}
