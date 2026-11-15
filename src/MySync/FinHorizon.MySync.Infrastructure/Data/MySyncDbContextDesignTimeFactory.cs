using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinHorizon.MySync.Infrastructure.Data;

public sealed class MySyncDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MySyncDbContext>
{
    // Local design-time placeholder used only by EF tooling. Runtime uses ConnectionStrings:mysync-db.
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=mysync-db;Username=fnhz_designtime;Password=fnhz_designtime";

    public MySyncDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<MySyncDbContext>().UseNpgsql(DesignTimeConnectionString).Options);
}
