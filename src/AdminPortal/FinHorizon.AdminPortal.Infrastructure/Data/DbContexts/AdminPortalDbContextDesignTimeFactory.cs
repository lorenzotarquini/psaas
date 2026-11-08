using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinHorizon.AdminPortal.Infrastructure.Data.DbContexts;

public sealed class AdminPortalDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AdminPortalDbContext>
{
    // Local design-time placeholder used only by EF tooling. Runtime uses ConnectionStrings:adminportal-db.
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=adminportal-db;Username=fnhz_designtime;Password=fnhz_designtime";

    public AdminPortalDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AdminPortalDbContext>();
        optionsBuilder.UseNpgsql(DesignTimeConnectionString);

        return new AdminPortalDbContext(optionsBuilder.Options);
    }
}
