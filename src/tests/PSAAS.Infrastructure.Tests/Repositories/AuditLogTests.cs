using Microsoft.Extensions.Time.Testing;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Infrastructure.Audit;
using Xunit;

namespace PSAAS.Infrastructure.Tests.Repositories;

/// <summary>Derived from UC12: recording with the actor and reading the log back with its filters.</summary>
public sealed class AuditLogTests
{
    private sealed class Actor : ICurrentActor
    {
        public string Id => "kc-marta";

        public string Name => "Marta Russo";
    }

    private readonly Data.DbContexts.PsaasDbContext _db = InMemoryDatabase.Create();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 22, 9, 0, 0, TimeSpan.Zero));

    private AuditLog Log => new(_db, new Actor(), _clock);

    private async Task RecordAsync(string action, string tenantId, string target)
    {
        await Log.RecordAsync(new AuditRecord(action, AuditTargets.Tenant, tenantId, target, tenantId, new { profiles = 2 }));
        _clock.Advance(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task UC12_S1_RecordsTheActorTheTimeAndTheDetails()
    {
        await RecordAsync(AuditActions.TenantCreated, "t1", "Alfa Srl");

        var e = Assert.Single((await Log.GetPageAsync(new AuditQuery())).Items);
        Assert.Equal(("kc-marta", "Marta Russo", "tenant.created", "Alfa Srl"), (e.ActorId, e.ActorName, e.Action, e.TargetName));
        Assert.Equal(new DateTimeOffset(2026, 10, 22, 9, 0, 0, TimeSpan.Zero), e.OccurredAt);
        Assert.Equal(2, e.Details!.Value.GetProperty("profiles").GetInt32());
    }

    [Fact]
    public async Task UC12_S5_S6_FiltersByTenantActionPrefixTextAndTime_NewestFirst()
    {
        await RecordAsync(AuditActions.TenantCreated, "t1", "Alfa Srl");
        await RecordAsync(AuditActions.TenantOrganizationUpdated, "t1", "Alfa Srl");
        await RecordAsync(AuditActions.TenantCreated, "t2", "Beta Snc");

        Assert.Equal(["tenant.organization.updated", "tenant.created"], (await Log.GetPageAsync(new AuditQuery(TenantId: "t1"))).Items.Select(e => e.Action));
        Assert.Equal(3, (await Log.GetPageAsync(new AuditQuery(Action: "tenant."))).TotalCount);
        Assert.Equal(["Beta Snc"], (await Log.GetPageAsync(new AuditQuery(Search: "BETA"))).Items.Select(e => e.TargetName));
        var from = new DateTimeOffset(2026, 10, 22, 9, 1, 0, TimeSpan.Zero);
        Assert.Equal(2, (await Log.GetPageAsync(new AuditQuery(From: from))).TotalCount);
    }

    [Fact]
    public async Task UC12_S7_Pages()
    {
        for (var i = 0; i < 3; i++)
        {
            await RecordAsync(AuditActions.TenantCreated, $"t{i}", $"Cliente {i}");
        }

        var page = await Log.GetPageAsync(new AuditQuery(Page: 2, PageSize: 2));

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(["Cliente 0"], page.Items.Select(e => e.TargetName));
    }
}
