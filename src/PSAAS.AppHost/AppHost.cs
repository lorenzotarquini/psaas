var builder = DistributedApplication.CreateBuilder(args);

var postgresUsername = builder.AddParameter("postgres-username");
var postgresPassword = builder.AddParameter("postgres-password");
var keycloakAuthority = builder.AddParameter("keycloak-authority");
var keycloakClientId = builder.AddParameter("keycloak-client-id");
var keycloakMode = builder.AddParameter("keycloak-mode");
var keycloakRequireHttpsMetadata = builder.AddParameter("keycloak-require-https-metadata");
var keycloakCallbackPath = builder.AddParameter("keycloak-callback-path");
var keycloakSignedOutCallbackPath = builder.AddParameter("keycloak-signed-out-callback-path");
var keycloakRemoteSignOutPath = builder.AddParameter("keycloak-remote-sign-out-path");
var keycloakRoleResourceClientId = builder.AddParameter("keycloak-role-resource-client-id");
var keycloakAdministratorPolicyName = builder.AddParameter("keycloak-administrator-policy-name");
var keycloakAdministratorRole = builder.AddParameter("keycloak-administrator-role");

var postgres = builder.AddPostgres("postgres", postgresUsername, postgresPassword, port: 5432)
    .WithDataBindMount("../postgres_data");

var psaasDb = postgres.AddDatabase("psaas-db", "psaas-db");

builder.AddProject<Projects.PSAAS_AdminPortal>("adminportal")
    .WithReference(psaasDb)
    .WaitFor(psaasDb)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__ClientId", keycloakClientId)
    .WithEnvironment("Keycloak__Mode", keycloakMode)
    .WithEnvironment("Keycloak__RequireHttpsMetadata", keycloakRequireHttpsMetadata)
    .WithEnvironment("Keycloak__CallbackPath", keycloakCallbackPath)
    .WithEnvironment("Keycloak__SignedOutCallbackPath", keycloakSignedOutCallbackPath)
    .WithEnvironment("Keycloak__RemoteSignOutPath", keycloakRemoteSignOutPath)
    .WithEnvironment("Keycloak__RoleResourceClientIds__0", keycloakRoleResourceClientId)
    .WithEnvironment("Keycloak__Policies__0__Name", keycloakAdministratorPolicyName)
    .WithEnvironment("Keycloak__Policies__0__Roles__0", keycloakAdministratorRole);

builder.Build().Run();
