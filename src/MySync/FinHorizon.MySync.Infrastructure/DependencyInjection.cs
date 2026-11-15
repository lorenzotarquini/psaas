using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Infrastructure.Currencies;
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
        services.AddScoped<IPreferencesRepository, PreferencesRepository>();

        // UC13, D10: catalog and rates from Frankfurter, kept in memory (R9).
        services.AddMemoryCache();
        services.AddOptions<CurrencyServiceOptions>()
            .Bind(configuration.GetSection(CurrencyServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHttpClient(FrankfurterCurrencies.ClientName, (provider, client) =>
        {
            client.BaseAddress = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CurrencyServiceOptions>>().Value.Url;
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<FrankfurterCurrencies>();
        services.AddSingleton<ICurrencyCatalog>(provider => provider.GetRequiredService<FrankfurterCurrencies>());
        services.AddSingleton<IExchangeRates>(provider => provider.GetRequiredService<FrankfurterCurrencies>());

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
