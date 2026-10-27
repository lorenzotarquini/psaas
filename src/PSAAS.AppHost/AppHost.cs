var builder = DistributedApplication.CreateBuilder(args);

var postgresUsername = builder.AddParameter("postgres-username");
var postgresPassword = builder.AddParameter("postgres-password");

var postgres = builder.AddPostgres("postgres", postgresUsername, postgresPassword, port: 5432)
    .WithDataBindMount("../postgres_data");

var psaasDb = postgres.AddDatabase("psaas-db", "psaas-db");

// Secret of the confidential client adminportal-api: its service account manages users, reads login events
// and exchanges tokens for impersonation. Set it with: dotnet user-secrets set "Parameters:admin-portal-client-secret" "<value>"
var keycloakClientSecret = builder.AddParameter("admin-portal-client-secret", secret: true);

var backend = builder.AddProject<Projects.PSAAS_AdminPortal_Backend>("adminportal-backend")
    .WithReference(psaasDb)
    .WaitFor(psaasDb)
    .WithEnvironment("Keycloak__ClientSecret", keycloakClientSecret)
    // Locally emails are written as .eml files instead of being sent: no mail reaches the clients' PECs.
    .WithEnvironment("Email__From", "noreply@psaas.local")
    .WithEnvironment("Email__PickupDirectory", Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../mail_pickup")));

// Fixed port: it must match the redirect URIs and web origins of the Keycloak client adminportal-frontend.
builder.AddViteApp("adminportal-frontend", "../PSAAS.AdminPortal/PSAAS.AdminPortal.Frontend")
    .WithEndpoint("http", endpoint => endpoint.Port = 5173)
    .WithEnvironment("BACKEND_URL", backend.GetEndpoint("https"))
    .WithExternalHttpEndpoints()
    .WaitFor(backend);

builder.Build().Run();
