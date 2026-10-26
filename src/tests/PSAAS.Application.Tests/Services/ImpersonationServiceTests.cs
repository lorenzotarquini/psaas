using PSAAS.Application.Abstractions.Impersonation;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Services.Overview;
using PSAAS.Application.Services.Users;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC31 (impersonificazione) and UC29-R2 (inviti non completati).</summary>
public sealed class ImpersonationServiceTests
{
    private sealed class FakeGateway : IImpersonationGateway
    {
        public bool IsConfigured { get; set; } = true;

        public List<(string UserId, TimeSpan Duration)> Sessions { get; } = [];

        public Task<string> CreateSessionUrlAsync(string userId, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            Sessions.Add((userId, duration));
            return Task.FromResult("https://platform.test/impersonate#x");
        }
    }

    private readonly FakeUserDirectory _directory = new();
    private readonly FakeProfileReadRepository _profiles = new();
    private readonly FakeAuditLog _audit = new();
    private readonly FakeGateway _gateway = new();

    private ImpersonationService Service
    {
        get
        {
            var rules = new UserRules(_profiles, _directory, new FakeSubscriptionRepository(), new FakeTenantReadRepository(), _audit);
            return new ImpersonationService(new UserManagementService(_directory, _profiles, rules, TimeProvider.System), _gateway, rules, TimeProvider.System);
        }
    }

    [Theory]
    [InlineData("troppo", 30, "reason")]
    [InlineData("Verifica segnalazione", 4, "minutes")]
    [InlineData("Verifica segnalazione", 61, "minutes")]
    public async Task UC31_R1_ReasonAndDuration_AreCheckedBeforeKeycloak(string reason, int minutes, string field)
    {
        _directory.Users.Add(new("u1", "Mario", "Rossi", "m@x.it", null, []));

        var result = await Service.StartAsync("u1", new(reason, minutes));

        Assert.Equal("InvalidValue", result.Errors[field]);
        Assert.Empty(_gateway.Sessions);
    }

    [Fact]
    public async Task UC31_DefaultDurationIs30Minutes_AndTheImpersonationIsAudited()
    {
        _directory.Users.Add(new("u1", "Mario", "Rossi", "m@x.it", null, []));

        var result = await Service.StartAsync("u1", new("Verifica segnalazione 4521", null));

        Assert.Equal(TimeSpan.FromMinutes(30), Assert.Single(_gateway.Sessions).Duration);
        Assert.NotNull(result.Ticket);
        Assert.Equal("user.impersonated", Assert.Single(_audit.Records).Action);
    }

    [Fact]
    public async Task UC31_1a_1b_NotConfiguredOrDisabledUser_IsAConflict()
    {
        _directory.Users.Add(new("u1", "Mario", "Rossi", "m@x.it", null, [], Enabled: false));
        _directory.Users.Add(new("u2", "Anna", "Bianchi", "a@x.it", null, []));

        var disabled = await Service.StartAsync("u1", new("Verifica segnalazione", 10));
        _gateway.IsConfigured = false;
        var notConfigured = await Service.StartAsync("u2", new("Verifica segnalazione", 10));

        Assert.True(disabled.Conflict && notConfigured.Conflict);
        Assert.False((await Service.StartAsync("nope", new("Verifica segnalazione", 10))).Found);
        Assert.Empty(_gateway.Sessions);
    }

    [Fact]
    public void UC29_R2_StaleInvites_OpenForMoreThan5DaysAndStillEnabled()
    {
        var now = new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);
        DirectoryUser User(string id, int daysAgo, bool pending = true, bool enabled = true) =>
            new(id, id, id, $"{id}@x.it", null, [], enabled, pending, LastInviteAt: now.AddDays(-daysAgo));

        var stale = UserManagementService.StaleInvites([User("old", 6), User("new", 1), User("active", 9, pending: false), User("off", 9, enabled: false)], now);

        Assert.Equal(["old"], stale.Select(u => u.Id));
    }
}
