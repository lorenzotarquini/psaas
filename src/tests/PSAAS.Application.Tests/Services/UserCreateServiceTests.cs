using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Users;
using PSAAS.Application.Services.Users;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC6 (Creare un utente).</summary>
public sealed class UserCreateServiceTests
{
    private readonly FakeProfileReadRepository _profiles = new();
    private readonly FakeAuditLog _audit = new();
    private readonly FakeUserDirectory _directory = new();

    private UserCreateService Service => new(_profiles, _directory, _audit);

    private static UserCreateRequest Valid(params string[] profileIds) =>
        new(" Mario ", " Rossi ", " Mario.Rossi@Example.com ", "1980-05-17", profileIds);

    [Fact]
    public async Task UC6_S1_ValidRequest_CreatesTheUserWithTrimmedValuesAndProfilesOfDifferentTenants()
    {
        _profiles.Profiles.Add(new("p1", "Operatore", "t1", "Alfa Srl"));
        _profiles.Profiles.Add(new("p2", "Revisore", "t2", "Beta Snc"));

        var result = await Service.CreateAsync(Valid(" p1 ", "p2", "p1"));

        Assert.True(result.Succeeded);
        var created = _directory.Created!;
        Assert.Equal(
            ("Mario", "Rossi", "mario.rossi@example.com", new DateOnly(1980, 5, 17)),
            (created.FirstName, created.LastName, created.Email, created.BirthDate));
        Assert.Equal(["p1", "p2"], created.ProfileIds);
        Assert.Equal(["Alfa Srl", "Beta Snc"], result.User!.Profiles.Select(profile => profile.TenantCompanyName));
    }

    [Fact]
    public async Task UC6_S2_NoProfiles_IsValid()
    {
        var result = await Service.CreateAsync(Valid() with { ProfileIds = null });

        Assert.True(result.Succeeded);
        Assert.Empty(_directory.Created!.ProfileIds);
    }

    [Fact]
    public async Task UC6_S3_MissingValues_AreRequiredAndNothingIsCreated()
    {
        var result = await Service.CreateAsync(new UserCreateRequest(" ", null, "", null));

        Assert.Equal(
            ["birthDate", "email", "firstName", "lastName"],
            result.Errors.Where(error => error.Value == "Required").Select(error => error.Key).Order());
        Assert.Null(_directory.Created);
    }

    [Theory]
    [InlineData("email", "non-una-email")]
    [InlineData("birthDate", "17/05/1980")]
    [InlineData("birthDate", "1980-02-30")]
    public async Task UC6_S4_MalformedValues_AreInvalidFormat(string field, string value)
    {
        var request = field == "email" ? Valid() with { Email = value } : Valid() with { BirthDate = value };

        var result = await Service.CreateAsync(request);

        Assert.Equal("InvalidFormat", result.Errors[field]);
        Assert.Null(_directory.Created);
    }

    [Fact]
    public async Task UC6_S5_EmailTakenInKeycloak_IsNotUniqueOnTheEmail()
    {
        _directory.EmailTaken = true;

        var result = await Service.CreateAsync(Valid());

        Assert.Equal("NotUnique", Assert.Single(result.Errors, error => error.Key == "email").Value);
    }

    [Fact]
    public async Task UC6_S6_UnknownProfile_IsInvalidValueAtItsPosition()
    {
        _profiles.Profiles.Add(new("p1", "Operatore", "t1", "Alfa Srl"));

        var result = await Service.CreateAsync(Valid("p1", "missing", ""));

        Assert.Equal(["profileIds[1]", "profileIds[2]"], result.Errors.Keys.Order());
        Assert.All(result.Errors.Values, code => Assert.Equal("InvalidValue", code));
        Assert.Null(_directory.Created);
    }

    [Fact]
    public async Task UC6_S7_KeycloakUnavailable_Propagates()
    {
        _directory.Unavailable = true;

        await Assert.ThrowsAsync<UserDirectoryUnavailableException>(() => Service.CreateAsync(Valid()));
    }

    [Fact]
    public async Task UC12_S4_OneEventPerTenantOfTheChosenProfiles()
    {
        _profiles.Profiles.Add(new("p1", "Operatore", "t1", "Alfa Srl"));
        _profiles.Profiles.Add(new("p2", "Revisore", "t2", "Beta Snc"));

        await Service.CreateAsync(Valid("p1", "p2"));

        Assert.Equal(["t1", "t2"], _audit.Records.Select(r => r.TenantId).Order());
        Assert.All(_audit.Records, r => Assert.Equal(("user.created", "Mario Rossi"), (r.Action, r.TargetName)));
    }
}
