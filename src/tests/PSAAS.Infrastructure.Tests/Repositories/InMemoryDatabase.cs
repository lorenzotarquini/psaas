using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PSAAS.Domain;
using PSAAS.Infrastructure.Converters;
using PSAAS.Infrastructure.Data.DbContexts;
using PSAAS.Infrastructure.Data.DbModels;

namespace PSAAS.Infrastructure.Tests.Repositories;

/// <summary>
/// EF InMemory database for repository unit tests.
/// shortcut: InMemory has no unique indexes, transactions or Postgres errors, so unique-violation
/// handling in TenantWriteRepository is not unit-tested; covered only if a Postgres-based test is added.
/// </summary>
public static class InMemoryDatabase
{
    public static PsaasDbContext Create() => new(new DbContextOptionsBuilder<PsaasDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        // The write repository opens a transaction: InMemory ignores it instead of failing.
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    public static TenantDbModel AddTenant(
        this PsaasDbContext db,
        string companyName,
        string completeVatNumber,
        string certifiedEmail,
        string city = "Roma")
    {
        var tenant = TenantDbModelConverter.ToDbModel(Tenant.Create(
            companyName, completeVatNumber[2..], completeVatNumber, certifiedEmail, new Address("Via Roma 1", city, "00118", "IT")));
        db.Tenants.Add(tenant);
        db.SaveChanges();
        return tenant;
    }

    public static ProfileDbModel AddProfile(this PsaasDbContext db, TenantDbModel tenant, string name)
    {
        var profile = new ProfileDbModel { Id = Guid.NewGuid().ToString(), TenantId = tenant.Id, Name = name };
        db.Profiles.Add(profile);
        db.SaveChanges();
        return profile;
    }

    public static ProductiveUnitDbModel AddUnit(this PsaasDbContext db, TenantDbModel tenant, string name, params ProfileDbModel[] openedBy)
    {
        var unit = new ProductiveUnitDbModel { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = name };
        db.ProductiveUnits.Add(unit);
        db.ProductiveUnitProfiles.AddRange(openedBy.Select(profile =>
            new ProductiveUnitProfileDbModel { ProductiveUnitId = unit.Id, ProfileId = profile.Id }));
        db.SaveChanges();
        return unit;
    }
}
