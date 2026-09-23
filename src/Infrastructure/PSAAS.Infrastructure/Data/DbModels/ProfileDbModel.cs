namespace PSAAS.Infrastructure.Data.DbModels;

public sealed class ProfileDbModel
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<ProductiveUnitDbModel> ProductiveUnits { get; set; } = [];
}
