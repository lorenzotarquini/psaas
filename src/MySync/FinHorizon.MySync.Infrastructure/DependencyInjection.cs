using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Infrastructure.Data;
using FinHorizon.MySync.Infrastructure.Persistence;
using FinHorizon.MySync.Infrastructure.Reading;
using FinHorizon.MySync.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinHorizon.MySync.Infrastructure;

public static class DependencyInjection
{
    private const string MySyncDbConnectionStringName = "mysync-db";

    public static IServiceCollection AddMySyncInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(MySyncDbConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing required connection string 'ConnectionStrings:{MySyncDbConnectionStringName}'.");

        services.AddDbContext<MySyncDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IDebtRepository, DebtRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ILedgerReader, LedgerReader>();
        services.AddScoped<ITransactionReader, TransactionReader>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICalendar, ZonedCalendar>();
        services.AddOptions<CalendarOptions>()
            .Bind(configuration.GetSection(CalendarOptions.SectionName))
            .Validate(options => TimeZoneInfo.TryFindSystemTimeZoneById(options.TimeZone, out _), "Calendar:TimeZone is not a known time zone.")
            .ValidateOnStart();
        // RF2, RF4: a wrong limit stops the backend at start, not at the first request.
        services.AddOptions<LimitsOptions>()
            .Bind(configuration.GetSection(LimitsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }
}
