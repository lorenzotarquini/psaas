using FinHorizon.AdminPortal.Application.Abstractions.Users;
using FinHorizon.AdminPortal.Application.Services.Users;
using FinHorizon.Modules.Sdk;
using Xunit;

namespace FinHorizon.AdminPortal.Application.Tests.Services;

/// <summary>Derived from UC32 (moduli dell'utente).</summary>
public sealed class UserModuleServiceTests
{
    private readonly FakeProfileReadRepository _profiles = new();
    private readonly FakeUserDirectory _directory = new();
    private readonly FakeAuditLog _audit = new();
    private readonly FakeCatalogRepository _catalog = new();

    public UserModuleServiceTests()
    {
        _directory.Users.Add(new DirectoryUser("u1", "Mario", "Rossi", "mario@example.com", []));
        _catalog.Modules.Add(FakeCatalogRepository.Module("mysync"));
    }

    private UserModuleService Service
    {
        get
        {
            var rules = new UserRules(_profiles, _directory, new FakeSubscriptionRepository(), new FakeTenantReadRepository(), _audit);
            return new(_directory, _catalog, new UserManagementService(_directory, _profiles, rules, TimeProvider.System), rules);
        }
    }

    [Fact]
    public async Task UC32_S1_GrantGivesTheModuleOnceAndRecordsIt()
    {
        var first = await Service.GrantAsync("u1", "mysync");
        var again = await Service.GrantAsync("u1", "mysync");

        Assert.Equal(["mysync"], first.ModuleIds);
        Assert.Equal(["mysync"], again.ModuleIds);
        Assert.Single(_audit.Records, r => r.Action == "user.module.granted");
    }

    [Fact]
    public async Task UC32_S2_RevokeTakesTheModuleAwayAndEndsTheSessions()
    {
        await Service.GrantAsync("u1", "mysync");

        var result = await Service.RevokeAsync("u1", "mysync");

        Assert.True(result.Succeeded);
        Assert.Empty(result.ModuleIds);
        Assert.Equal(["u1"], _directory.SignedOut);
        Assert.Single(_audit.Records, r => r.Action == "user.module.revoked");
    }

    [Fact]
    public async Task UC32_RevokeOfAModuleTheUserDoesNotHave_ChangesNothing()
    {
        var result = await Service.RevokeAsync("u1", "mysync");

        Assert.True(result.Succeeded);
        Assert.Empty(_directory.SignedOut);
        Assert.Empty(_audit.Records);
    }

    [Theory]
    [InlineData("nope")]
    [InlineData("old")]
    public async Task UC32_S3_UnknownOrDeprecatedModule_IsInvalidValue(string moduleId)
    {
        _catalog.Modules.Add(FakeCatalogRepository.Module("old", status: ModuleStatuses.Deprecated));

        var result = await Service.GrantAsync("u1", moduleId);

        Assert.Equal("InvalidValue", result.Errors["moduleId"]);
        Assert.Empty(_directory.Modules);
    }

    [Fact]
    public async Task UC32_S4_UnknownUser_IsNotFound()
    {
        Assert.False((await Service.GrantAsync("nobody", "mysync")).Found);
        Assert.False((await Service.RevokeAsync("nobody", "mysync")).Found);
        Assert.Null(await Service.GetAsync("nobody"));
    }
}
