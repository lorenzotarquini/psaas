namespace PSAAS.Infrastructure.Data.DbModels;

public sealed class ProductiveUnitProfileDbModel
{
    public Guid ProductiveUnitId { get; set; }

    public ProductiveUnitDbModel? ProductiveUnit { get; set; }

    public string ProfileId { get; set; } = string.Empty;

    public ProfileDbModel? Profile { get; set; }
}
