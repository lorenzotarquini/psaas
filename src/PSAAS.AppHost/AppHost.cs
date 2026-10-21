var builder = DistributedApplication.CreateBuilder(args);

var postgresUsername = builder.AddParameter("postgres-username");
var postgresPassword = builder.AddParameter("postgres-password");

var postgres = builder.AddPostgres("postgres", postgresUsername, postgresPassword, port: 5432)
    .WithDataBindMount("../postgres_data");

var psaasDb = postgres.AddDatabase("psaas-db", "psaas-db");

var backend = builder.AddProject<Projects.PSAAS_AdminPortal_Backend>("adminportal-backend")
    .WithReference(psaasDb)
    .WaitFor(psaasDb);

// Fixed port: it must match the redirect URIs and web origins of the Keycloak client adminportal-frontend.
builder.AddViteApp("adminportal-frontend", "../PSAAS.AdminPortal/PSAAS.AdminPortal.Frontend")
    .WithEndpoint("http", endpoint => endpoint.Port = 5173)
    .WithEnvironment("BACKEND_URL", backend.GetEndpoint("https"))
    .WithExternalHttpEndpoints()
    .WaitFor(backend);

builder.Build().Run();
