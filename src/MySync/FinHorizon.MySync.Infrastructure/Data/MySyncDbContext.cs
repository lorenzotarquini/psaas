using Microsoft.EntityFrameworkCore;

namespace FinHorizon.MySync.Infrastructure.Data;

/// <summary>The MySync database (mysync-db). Empty for now: tables arrive with the first entities and their migration.</summary>
public sealed class MySyncDbContext(DbContextOptions<MySyncDbContext> options) : DbContext(options);
