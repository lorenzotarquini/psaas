var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataBindMount("../postgres_data");

var psaasDb = postgres.AddDatabase("psaas-db", "psaas_db");

builder.AddProject<Projects.PSAAS_AdminPortal>("adminportal")
    .WithReference(psaasDb, "psaas_db")
    .WaitFor(psaasDb)
    .WithEnvironment("Keycloak__Authority", GetRequiredConfigurationValue("Keycloak:Authority"))
    .WithEnvironment("Keycloak__ClientId", GetRequiredConfigurationValue("Keycloak:ClientId"))
    .WithEnvironment("Keycloak__Mode", GetRequiredConfigurationValue("Keycloak:Mode"))
    .WithEnvironment("Keycloak__RequireHttpsMetadata", GetRequiredConfigurationValue("Keycloak:RequireHttpsMetadata"))
    .WithEnvironment("Keycloak__CallbackPath", GetRequiredConfigurationValue("Keycloak:CallbackPath"))
    .WithEnvironment("Keycloak__SignedOutCallbackPath", GetRequiredConfigurationValue("Keycloak:SignedOutCallbackPath"))
    .WithEnvironment("Keycloak__RemoteSignOutPath", GetRequiredConfigurationValue("Keycloak:RemoteSignOutPath"))
    .WithEnvironment("Keycloak__RoleResourceClientIds__0", GetRequiredConfigurationValue("Keycloak:RoleResourceClientIds:0"))
    .WithEnvironment("Keycloak__Policies__0__Name", GetRequiredConfigurationValue("Keycloak:Policies:0:Name"))
    .WithEnvironment("Keycloak__Policies__0__Roles__0", GetRequiredConfigurationValue("Keycloak:Policies:0:Roles:0"));

builder.Build().Run();

string GetRequiredConfigurationValue(string key)
{
    return builder.Configuration[key]
        ?? throw new InvalidOperationException($"Missing required AppHost configuration value '{key}'.");
}
