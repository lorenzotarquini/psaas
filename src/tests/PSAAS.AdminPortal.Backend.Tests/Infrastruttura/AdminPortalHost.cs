using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Abstractions.Communications;
using PSAAS.Application.Abstractions.Geography;
using PSAAS.Application.Abstractions.Users;

namespace PSAAS.AdminPortal.Backend.Tests.Infrastruttura;

/// <summary>
/// The real backend on the real Postgres; only the external systems (Keycloak, city list) are simulated.
/// </summary>
public sealed class AdminPortalHost(string connectionString) : WebApplicationFactory<Program>
{
    public FakeKeycloakUsers KeycloakUsers { get; } = new();

    public FakeItalianCityList CityList { get; } = new();

    public FakeModuleHooks ModuleHooks { get; } = new();

    public FakeMailbox Mailbox { get; } = new();

    /// <summary>
    /// The clock of the backend: scenarios move it forward (due dates, renewals). It ticks by itself so events keep their order.
    /// </summary>
    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow) { AutoAdvanceAmount = TimeSpan.FromMilliseconds(1) };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:psaas-db", connectionString);
        // Scenarios drive retries themselves.
        builder.UseSetting("Provisioning:RetryMinutes", "0");
        builder.UseSetting("Billing:RunHours", "0");
        builder.ConfigureTestServices(services =>
        {
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Static configuration: the backend trusts the test signing key and never calls Keycloak.
                options.Configuration = new OpenIdConnectConfiguration { Issuer = KeycloakTokens.Issuer };
                options.Configuration.SigningKeys.Add(KeycloakTokens.SigningKey);
                // Tokens are signed with the real time: the moved clock of the scenarios must not expire them.
                options.TimeProvider = TimeProvider.System;
            });
            services.AddSingleton<TimeProvider>(Clock);
            services.AddSingleton<IUserDirectory>(KeycloakUsers);
            services.AddSingleton<IItalianCityProvider>(CityList);
            services.AddSingleton<IModuleProvisioner>(ModuleHooks);
            services.AddSingleton<IEmailSender>(Mailbox);
        });
    }
}
