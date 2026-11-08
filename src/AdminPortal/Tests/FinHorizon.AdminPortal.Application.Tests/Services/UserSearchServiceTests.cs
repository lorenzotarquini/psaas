using FinHorizon.AdminPortal.Application.Abstractions.Users;
using FinHorizon.AdminPortal.Application.Services.Users;
using Xunit;

namespace FinHorizon.AdminPortal.Application.Tests.Services;

/// <summary>Derived from UC7 (Cercare gli utenti).</summary>
public sealed class UserSearchServiceTests
{
    private readonly FakeProfileReadRepository _profiles = new();
    private readonly FakeUserDirectory _directory = new();

    private UserSearchService Service => new(_directory, _profiles);

    [Fact]
    public async Task UC7_S1_S4_ProfilesComeWithTheirTenant_DeletedOnesAreLeftOut()
    {
        _profiles.Profiles.Add(new("p1", "Operatore", "t1", "Alfa Srl"));
        _directory.Users.Add(new("kc-1", "Mario", "Rossi", "mario.rossi@example.com", ["p1", "deleted"]));

        var mario = Assert.Single((await Service.SearchAsync(new UserQuery("mario", null, null, null))).Items);

        Assert.Equal(("Operatore", "Alfa Srl"), (Assert.Single(mario.Profiles).Name, mario.Profiles[0].TenantCompanyName));
    }

    [Fact]
    public async Task UC7_S6_KeycloakUnavailable_Propagates()
    {
        _directory.Unavailable = true;

        await Assert.ThrowsAsync<UserDirectoryUnavailableException>(() => Service.SearchAsync(new UserQuery("mario", null, null, null)));
    }
}
