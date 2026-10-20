namespace PSAAS.Infrastructure.Data.DbModels;

public sealed class UserDbModel
{
    public Guid Id { get; set; }

    public string KeycloakId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? TenantId { get; set; }

    public TenantDbModel? Tenant { get; set; }

    public string? ProfileId { get; set; }

    public ProfileDbModel? Profile { get; set; }
}
