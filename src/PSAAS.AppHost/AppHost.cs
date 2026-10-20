var builder = DistributedApplication.CreateBuilder(args);

var postgresUsername = builder.AddParameter("postgres-username");
var postgresPassword = builder.AddParameter("postgres-password");

var postgres = builder.AddPostgres("postgres", postgresUsername, postgresPassword, port: 5432)
    .WithDataBindMount("../postgres_data");

var psaasDb = postgres.AddDatabase("psaas-db", "psaas-db");

builder.AddProject<Projects.PSAAS_AdminPortal_Backend>("adminportal-backend")
    .WithReference(psaasDb)
    .WaitFor(psaasDb);

builder.Build().Run();
