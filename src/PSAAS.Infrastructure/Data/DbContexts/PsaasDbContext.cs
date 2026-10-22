using Microsoft.EntityFrameworkCore;
using PSAAS.Infrastructure.Audit;
using PSAAS.Infrastructure.Catalog;
using PSAAS.Infrastructure.Data.DbModels;

namespace PSAAS.Infrastructure.Data.DbContexts;

public sealed class PsaasDbContext(DbContextOptions<PsaasDbContext> options) : DbContext(options)
{
    public DbSet<TenantDbModel> Tenants => Set<TenantDbModel>();

    public DbSet<ProductiveUnitDbModel> ProductiveUnits => Set<ProductiveUnitDbModel>();

    public DbSet<ProfileDbModel> Profiles => Set<ProfileDbModel>();

    public DbSet<ProductiveUnitProfileDbModel> ProductiveUnitProfiles => Set<ProductiveUnitProfileDbModel>();

    public DbSet<AuditEventDbModel> AuditEvents => Set<AuditEventDbModel>();

    public DbSet<ModuleDbModel> Modules => Set<ModuleDbModel>();

    public DbSet<PlanDbModel> Plans => Set<PlanDbModel>();

    public DbSet<SubscriptionDbModel> Subscriptions => Set<SubscriptionDbModel>();

    public DbSet<TenantModuleDbModel> TenantModules => Set<TenantModuleDbModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("Tenants");

            entityBuilder.HasKey(tenant => tenant.Id);

            entityBuilder.HasIndex(tenant => tenant.Id).IsUnique();
            entityBuilder.HasIndex(tenant => tenant.NormalizedCompanyName).IsUnique();
            entityBuilder.HasIndex(tenant => tenant.CompleteVatNumber).IsUnique();
            entityBuilder.HasIndex(tenant => tenant.NormalizedCertifiedEmail).IsUnique();

            entityBuilder.Property(tenant => tenant.Id).IsRequired();
            entityBuilder.Property(tenant => tenant.CompanyName).IsRequired();
            entityBuilder.Property(tenant => tenant.NormalizedCompanyName).IsRequired();
            entityBuilder.Property(tenant => tenant.VatNumber).IsRequired();
            entityBuilder.Property(tenant => tenant.CompleteVatNumber).IsRequired();
            entityBuilder.Property(tenant => tenant.CertifiedEmail).IsRequired();
            entityBuilder.Property(tenant => tenant.NormalizedCertifiedEmail).IsRequired();

            entityBuilder.Property(tenant => tenant.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
            entityBuilder.Property(tenant => tenant.StatusChangedAt).HasDefaultValueSql("now()");
            entityBuilder.Property(tenant => tenant.CreatedAt).HasDefaultValueSql("now()");
            entityBuilder.Property(tenant => tenant.SdiCode).HasMaxLength(7);
            entityBuilder.HasIndex(tenant => tenant.Status);

            entityBuilder.OwnsOne(tenant => tenant.Address, addressBuilder =>
            {
                addressBuilder.ToJson(nameof(TenantDbModel.Address));
            });
        });

        modelBuilder.Entity<ProductiveUnitDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("ProductiveUnits");

            entityBuilder.HasKey(productiveUnit => productiveUnit.Id);

            entityBuilder.HasIndex(productiveUnit => new { productiveUnit.TenantId, productiveUnit.Name }).IsUnique();

            entityBuilder.Property(productiveUnit => productiveUnit.Id).IsRequired();
            entityBuilder.Property(productiveUnit => productiveUnit.Name).IsRequired();
            entityBuilder.Property(productiveUnit => productiveUnit.Description).IsRequired(false);
            entityBuilder.Property(productiveUnit => productiveUnit.TenantId).IsRequired();
            entityBuilder
                .HasOne(productiveUnit => productiveUnit.Tenant)
                .WithMany()
                .HasForeignKey(productiveUnit => productiveUnit.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductiveUnitProfileDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("ProductiveUnitProfiles");

            entityBuilder.HasKey(link => new { link.ProductiveUnitId, link.ProfileId });

            entityBuilder.Property(link => link.ProductiveUnitId).IsRequired();
            entityBuilder.Property(link => link.ProfileId).IsRequired();

            entityBuilder
                .HasOne(link => link.ProductiveUnit)
                .WithMany(productiveUnit => productiveUnit.ProductiveUnitProfiles)
                .HasForeignKey(link => link.ProductiveUnitId)
                .OnDelete(DeleteBehavior.Cascade);

            entityBuilder
                .HasOne(link => link.Profile)
                .WithMany(profile => profile.ProductiveUnitProfiles)
                .HasForeignKey(link => link.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProfileDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("Profiles");

            entityBuilder.HasKey(profile => profile.Id);

            entityBuilder.HasIndex(profile => profile.Id).IsUnique();
            entityBuilder.HasIndex(profile => new { profile.TenantId, profile.Name }).IsUnique();

            entityBuilder.Property(profile => profile.Id).IsRequired();
            entityBuilder.Property(profile => profile.Name).IsRequired();
            entityBuilder.Property(profile => profile.Description).IsRequired(false);
            entityBuilder.Property(profile => profile.TenantId).IsRequired();
            entityBuilder
                .HasOne<TenantDbModel>()
                .WithMany()
                .HasForeignKey(profile => profile.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Append-only: no foreign keys, so the history outlives what it talks about.
        modelBuilder.Entity<AuditEventDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("AuditEvents");
            entityBuilder.HasKey(auditEvent => auditEvent.Id);
            entityBuilder.HasIndex(auditEvent => auditEvent.OccurredAt);
            entityBuilder.HasIndex(auditEvent => new { auditEvent.TenantId, auditEvent.OccurredAt });
            entityBuilder.Property(auditEvent => auditEvent.ActorId).IsRequired();
            entityBuilder.Property(auditEvent => auditEvent.ActorName).IsRequired();
            entityBuilder.Property(auditEvent => auditEvent.Action).IsRequired();
            entityBuilder.Property(auditEvent => auditEvent.TargetType).IsRequired();
            entityBuilder.Property(auditEvent => auditEvent.TargetId).IsRequired();
            entityBuilder.Property(auditEvent => auditEvent.TargetName).IsRequired();
            entityBuilder.Property(auditEvent => auditEvent.Details).HasColumnType("jsonb");
        });

        // EXP-01/02/03: catalog, plans and the subscription of each tenant.
        modelBuilder.Entity<ModuleDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("Modules");
            entityBuilder.HasKey(module => module.Id);
            entityBuilder.Property(module => module.Name).IsRequired();
            entityBuilder.Property(module => module.Version).IsRequired();
            entityBuilder.Property(module => module.Status).IsRequired().HasMaxLength(20);
            // Core is always there (it is in every plan).
            entityBuilder.HasData(new ModuleDbModel
            {
                Id = "core",
                Name = "Core",
                Description = "Base comune della piattaforma",
                Icon = "cube",
                Status = "Available",
                Version = "2.0.3",
                IsCore = true,
                UpdatedAt = new DateTimeOffset(2026, 10, 22, 0, 0, 0, TimeSpan.Zero),
            });
        });

        modelBuilder.Entity<PlanDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("Plans");
            entityBuilder.HasKey(plan => plan.Id);
            entityBuilder.HasIndex(plan => plan.Code).IsUnique();
            entityBuilder.Property(plan => plan.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<SubscriptionDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("Subscriptions");
            entityBuilder.HasKey(subscription => subscription.TenantId);
            entityBuilder.Property(subscription => subscription.Price).HasPrecision(10, 2);
            entityBuilder.HasIndex(subscription => subscription.PlanId);
            entityBuilder.HasOne<TenantDbModel>().WithOne().HasForeignKey<SubscriptionDbModel>(s => s.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantModuleDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("TenantModules");
            entityBuilder.HasKey(module => new { module.TenantId, module.ModuleId });
            entityBuilder.HasOne<TenantDbModel>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Cascade);
            entityBuilder.HasOne<ModuleDbModel>().WithMany().HasForeignKey(m => m.ModuleId).OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }
}
