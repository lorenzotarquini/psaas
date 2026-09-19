using PSAAS.Domain;
using Xunit;

namespace PSAAS.Auth.Tests;

public sealed class UserTests
{
    [Fact]
    public void Create_SetsKeycloakAndUserData()
    {
        var user = User.Create("keycloak-user-id", "m.rossi", "m.rossi@example.com", "Mario", "Rossi");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("keycloak-user-id", user.KeycloakId);
        Assert.Equal("m.rossi", user.UserName);
        Assert.Equal("m.rossi@example.com", user.Email);
        Assert.Equal("Mario", user.FirstName);
        Assert.Equal("Rossi", user.LastName);
        Assert.Empty(user.Roles);
        Assert.Empty(user.Claims);
    }

    [Fact]
    public void AddRoleAndClaim_StoresDistinctValuesIgnoringCase()
    {
        var user = User.Create("keycloak-user-id", "m.rossi", "m.rossi@example.com", "Mario", "Rossi");

        user.AddRole("SystemAdmin");
        user.AddRole("systemadmin");
        user.AddClaim("tenant_id", "tenant-1");
        user.AddClaim("TENANT_ID", "tenant-1");

        Assert.Single(user.Roles);
        Assert.True(user.HasRole("systemadmin"));
        Assert.True(user.HasClaim("tenant_id", "tenant-1"));
        Assert.Single(user.Claims["tenant_id"]);
    }

    [Fact]
    public void AddToTenantAndAddProfile_SetDomainRelations()
    {
        var tenantId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var user = User.Create("keycloak-user-id", "m.rossi", "m.rossi@example.com", "Mario", "Rossi");

        user.AddToTenant(tenantId);
        user.AddProfile(profileId);

        Assert.Equal(tenantId, user.TenantId);
        Assert.Equal(profileId, user.ProfileId);
    }
}
