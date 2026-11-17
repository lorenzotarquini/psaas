using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace FinHorizon.MySync.Backend.Tests.Infrastruttura;

/// <summary>The real backend on the real Postgres; only Keycloak (the token issuer) and the clock are simulated.</summary>
public sealed class MySyncHost(string connectionString, IReadOnlyDictionary<string, string>? settings = null) : WebApplicationFactory<Program>
{
    /// <summary>"Today" is 2026-11-15 in Rome; scenarios move it to build a history (UC7).</summary>
    public ScenarioClock Clock { get; } = new();

    public FakeCurrencies Currencies { get; } = new();

    /// <summary>UC17: Stripe.</summary>
    public FakePayments Payments { get; } = new();

    public const string FrontendUrl = "http://mysync.test";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:mysync-db", connectionString);
        builder.UseSetting("Currencies:Url", "https://frankfurter.test/");
        builder.UseSetting("Stripe:WebhookSecret", StripeEvents.Secret);
        builder.UseSetting("Membership:FrontendUrl", FrontendUrl);
        foreach (var (key, value) in settings ?? new Dictionary<string, string>())
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Static configuration: the backend trusts the test signing key and never calls Keycloak.
                options.Configuration = new OpenIdConnectConfiguration { Issuer = KeycloakTokens.Issuer };
                options.Configuration.SigningKeys.Add(KeycloakTokens.SigningKey);
                options.TimeProvider = TimeProvider.System;
            });
            services.AddSingleton<TimeProvider>(Clock);
            // UC13: the scenarios never call Frankfurter.
            services.AddSingleton<Application.Abstractions.Currencies.ICurrencyCatalog>(Currencies);
            services.AddSingleton<Application.Abstractions.Currencies.IExchangeRates>(Currencies);
            services.AddSingleton<Application.Abstractions.Payments.IPaymentProvider>(Payments);
        });
    }
}
