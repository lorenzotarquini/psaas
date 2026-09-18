# PSAAS.Auth

Reusable Keycloak authentication library for PSAAS .NET services.

Public entry point: `services.AddKeycloak(...)`.

## API / backend with JWT bearer

```csharp
using PSAAS.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKeycloak(options =>
{
    options.Authority = "https://keycloak.example/realms/psaas";
    options.ClientId = "psaas-api";
    options.Mode = KeycloakAuthenticationMode.Api;
    options.RoleResourceClientIds = ["psaas-api"];
    options.Policies.Add(new KeycloakPolicyOptions
    {
        Name = "AdminOnly",
        Roles = ["SystemAdmin"]
    });
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/admin", () => Results.Ok()).RequireAuthorization("AdminOnly");
app.Run();
```

## Web / server-side app with cookie + OpenID Connect

```csharp
builder.Services.AddKeycloak(options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"]!;
    options.ClientId = builder.Configuration["Keycloak:ClientId"]!;
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"]!;
    options.Mode = KeycloakAuthenticationMode.Web;
});
```

Keep `ClientSecret` in user secrets, environment variables, Key Vault, or an equivalent secure provider.
PSAAS.Auth never writes secrets or token bodies to logs.

## Machine-to-machine client credentials

```csharp
builder.Services.AddKeycloak(options =>
{
    options.Authority = "https://keycloak.example/realms/psaas";
    options.ClientId = "psaas-worker";
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"]!;
    options.Mode = KeycloakAuthenticationMode.MachineToMachine;
    options.ClientCredentials.HttpClientName = "DownstreamApi";
});

// Inject IHttpClientFactory and call CreateClient("DownstreamApi").
// Requests sent with this client receive a cached client_credentials bearer token.
```

## appsettings.json binding

```json
{
  "Keycloak": {
    "Authority": "https://keycloak.example/realms/psaas",
    "ClientId": "psaas-api",
    "Mode": "Api",
    "RoleResourceClientIds": [ "psaas-api" ]
  }
}
```

```csharp
builder.Services.AddKeycloak(builder.Configuration);
```
