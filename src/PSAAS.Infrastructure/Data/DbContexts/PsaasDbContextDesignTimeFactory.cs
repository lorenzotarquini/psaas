using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PSAAS.Infrastructure.Data.DbContexts;

public sealed class PsaasDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PsaasDbContext>
{
    // Local design-time placeholder used only by EF tooling. Runtime uses ConnectionStrings:psaas_db.
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=psaas_db;Username=psaas_designtime;Password=psaas_designtime";

    public PsaasDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PsaasDbContext>();
        optionsBuilder.UseNpgsql(DesignTimeConnectionString);

        return new PsaasDbContext(optionsBuilder.Options);
    }
}
