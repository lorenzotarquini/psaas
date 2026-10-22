using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Users;
using PSAAS.Application.Services.Users;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC19 (gestire un utente) and UC20 (importare utenti).</summary>
public sealed class UserManagementServiceTests
{
    private readonly FakeProfileReadRepository _profiles = new();
    private readonly FakeUserDirectory _directory = new();
    private readonly FakeAuditLog _audit = new();
    private readonly FakeSubscriptionRepository _subscriptions = new();

    private UserRules Rules => new(_profiles, _directory, _subscriptions, new FakeTenantReadRepository(), _audit);

    private UserManagementService Service => new(_directory, _profiles, Rules, TimeProvider.System);

    private DirectoryUser GivenUser(params string[] profileIds)
    {
        var user = new DirectoryUser("u1", "Mario", "Rossi", "mario@example.com", new DateOnly(1980, 1, 1), profileIds);
        _directory.Users.Add(user);
        return user;
    }

    [Theory]
    [InlineData(false, false, "Active", "Disabled")]
    [InlineData(true, true, "Active", "Invited")]
    [InlineData(true, false, "Suspended", "Suspended")]
    [InlineData(true, false, "Ceased", "Suspended")]
    [InlineData(true, false, "Active", "Active")]
    public void UC19_R1_StatusFromKeycloakAndTenant(bool enabled, bool passwordPending, string tenantStatus, string expected)
    {
        var user = new DirectoryUser("u1", "M", "R", "m@x.it", null, ["p1"], enabled, passwordPending);
        var profiles = new Dictionary<string, UserProfileReadModel> { ["p1"] = new("p1", "Operatore", "t1", "Alfa Srl", tenantStatus) };

        Assert.Equal(expected, UserReadModel.From(user, profiles).Status);
    }

    [Fact]
    public async Task UC19_S2_UpdateRecordsOnlyTheChangesInEveryTenantInvolved()
    {
        _profiles.Profiles.Add(new("p1", "Operatore", "t1", "Alfa Srl"));
        _profiles.Profiles.Add(new("p2", "Revisore", "t2", "Beta Snc"));
        GivenUser("p1");

        var result = await Service.UpdateAsync("u1", new("Mario", " Russo ", "1980-01-01", ["p2"], true));

        Assert.Equal(("Russo", true), (result.User!.LastName, result.User.MfaRequired));
        Assert.Equal(["t1", "t2"], _audit.Records.Select(r => r.TenantId).Order());
        Assert.All(_audit.Records, r => Assert.Equal("user.updated", r.Action));
    }

    [Fact]
    public async Task UC19_S2_SavingTheSameValues_RecordsNothing()
    {
        GivenUser();

        await Service.UpdateAsync("u1", new("Mario", "Rossi", "1980-01-01", [], false));

        Assert.Empty(_audit.Records);
    }

    [Fact]
    public async Task UC19_LimitIsCheckedOnlyForNewTenants()
    {
        var tenant = Guid.NewGuid().ToString();
        _profiles.Profiles.Add(new("p1", "Operatore", tenant, "Alfa Srl"));
        _subscriptions.Subscriptions[Guid.Parse(tenant)] = new(
            Guid.Parse(tenant), "Alfa Srl", Guid.NewGuid(), "Base", "Active", 10, "Monthly", 1, null,
            DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow, true, [], []);
        GivenUser("p1");

        var result = await Service.UpdateAsync("u1", new("Mario", "Rossi", "1980-01-01", ["p1"], false));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task UC19_S4_S5_InviteOnlyWhileTheInvitationIsOpen()
    {
        _directory.Users.Add(new("u1", "Giulia", "Ferri", "g@x.it", null, [], PasswordPending: true, InvitesSent: 1));
        _directory.Users.Add(new("u2", "Mario", "Rossi", "m@x.it", null, []));

        var invited = await Service.InviteAsync("u1");
        var refused = await Service.InviteAsync("u2");

        Assert.Equal(2, invited.User!.InvitesSent);
        Assert.True(refused.Conflict);
        Assert.Equal([("u1", "invite")], _directory.ActionEmails);
    }

    [Fact]
    public async Task UC19_S6_DisableTwice_IsAConflict_UnknownUserIsNotFound()
    {
        GivenUser();

        Assert.Equal("Disabled", (await Service.SetEnabledAsync("u1", false)).User!.Status);
        Assert.True((await Service.SetEnabledAsync("u1", false)).Conflict);
        Assert.False((await Service.ResetPasswordAsync("nope")).Found);
    }

    [Fact]
    public async Task UC20_PreviewMapsProfilesByNameAndFlagsRepeatedEmails()
    {
        _profiles.All.Add(new("p1", "t1", "Alfa Srl", "Operatore", null));
        _profiles.Profiles.Add(new("p1", "Operatore", "t1", "Alfa Srl"));
        var import = new UserImportService(_profiles, new UserCreateService(Rules, _directory, new FakeCurrentActor(), TimeProvider.System));
        UserImportRow Row(string email, string profiles) => new("Mario", "Rossi", email, "1980-01-01", profiles);

        var rows = await import.ImportAsync(new([Row("m@x.it", " alfa srl : OPERATORE "), Row("M@x.it", ""), Row("a@x.it", "Alfa Srl:Nessuno")], false));

        Assert.Empty(rows![0].Errors);
        Assert.Equal("NotUnique", rows[1].Errors["email"]);
        Assert.Equal("InvalidValue", rows[2].Errors["profiles"]);
        Assert.Empty(_directory.Users);
    }

    [Fact]
    public async Task UC20_R3_EmptyOrTooLongFile_IsRejected()
    {
        var import = new UserImportService(_profiles, new UserCreateService(Rules, _directory, new FakeCurrentActor(), TimeProvider.System));

        Assert.Null(await import.ImportAsync(new([], false)));
        Assert.Null(await import.ImportAsync(new(Enumerable.Repeat(new UserImportRow("a", "b", "c@x.it", "1980-01-01", ""), 501).ToArray(), false)));
    }
}
