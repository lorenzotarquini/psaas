using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace FinHorizon.MySync.Infrastructure.Tests;

/// <summary>RF2, RF4 and RNF4: configuration read and checked at start.</summary>
public sealed class ConfigurationTests
{
    [Fact]
    public void Limits_DefaultToTwoAccountsAndOneDebt()
    {
        using var provider = Provider([]);

        var limits = provider.GetRequiredService<IOptions<LimitsOptions>>().Value;

        Assert.Equal((2, 1), (limits.MaxAccounts, limits.MaxDebts));
    }

    [Fact]
    public void Limits_ComeFromTheMySyncSection()
    {
        using var provider = Provider(new() { ["MySync:MaxAccounts"] = "7", ["MySync:MaxDebts"] = "0" });

        var limits = provider.GetRequiredService<IOptions<LimitsOptions>>().Value;

        Assert.Equal((7, 0), (limits.MaxAccounts, limits.MaxDebts));
    }

    [Theory]
    [InlineData("MySync:MaxAccounts", "0")]
    [InlineData("MySync:MaxDebts", "-1")]
    public void Limits_OutOfRangeAreRefused(string key, string value)
    {
        using var provider = Provider(new() { [key] = value });

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<LimitsOptions>>().Value);
    }

    [Fact]
    public void Calendar_TodayIsInTheConfiguredTimeZone()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 15, 23, 30, 0, TimeSpan.Zero));
        using var provider = Provider([], clock);

        Assert.Equal(new DateOnly(2026, 11, 16), provider.GetRequiredService<ICalendar>().Today);
    }

    [Fact]
    public void Calendar_UnknownTimeZoneIsRefused()
    {
        using var provider = Provider(new() { ["Calendar:TimeZone"] = "Mars/Olympus" });

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<CalendarOptions>>().Value);
    }

    private static ServiceProvider Provider(Dictionary<string, string?> settings, TimeProvider? clock = null)
    {
        settings["ConnectionStrings:mysync-db"] = "Host=localhost;Database=unused";
        var services = new ServiceCollection().AddMySyncInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        if (clock is not null)
        {
            services.AddSingleton(clock);
        }

        return services.BuildServiceProvider();
    }
}
