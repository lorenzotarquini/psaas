var builder = DistributedApplication.CreateBuilder(args);

var postgresUsername = builder.AddParameter("postgres-username");
var postgresPassword = builder.AddParameter("postgres-password");

var postgres = builder.AddPostgres("postgres", postgresUsername, postgresPassword, port: 5432)
    .WithDataBindMount("../postgres_data");

var adminPortalDb = postgres.AddDatabase("adminportal-db", "adminportal-db");
var mySyncDb = postgres.AddDatabase("mysync-db", "mysync-db");

// Secret of the confidential client adminportal-api: its service account manages users, reads login events
// and exchanges tokens for impersonation. Set it with: dotnet user-secrets set "Parameters:admin-portal-client-secret" "<value>"
var keycloakClientSecret = builder.AddParameter("admin-portal-client-secret", secret: true);

var adminPortalBackend = builder.AddProject<Projects.FinHorizon_AdminPortal_Backend>("adminportal-backend")
    .WithReference(adminPortalDb)
    .WaitFor(adminPortalDb)
    .WithEnvironment("Keycloak__ClientSecret", keycloakClientSecret)
    // Locally emails are written as .eml files instead of being sent: no mail reaches the clients' PECs.
    .WithEnvironment("Email__From", "noreply@finhorizon.local")
    .WithEnvironment("Email__PickupDirectory", Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../mail_pickup")));

// Fixed port: it must match the redirect URIs and web origins of the Keycloak client adminportal-frontend.
builder.AddViteApp("adminportal-frontend", "../AdminPortal/FinHorizon.AdminPortal.Frontend")
    .WithEndpoint("http", endpoint => endpoint.Port = 5173)
    .WithEnvironment("BACKEND_URL", adminPortalBackend.GetEndpoint("https"))
    .WithExternalHttpEndpoints()
    .WaitFor(adminPortalBackend);

// MySync: its own database, backend and frontend; users sign in with the same realm.
// MySync settings, all here (Parameters in appsettings.json): accounts and debts per user (UC2-R6, UC3-R5),
// the time zone that decides "today", and the migrations applied at start.
var mySyncMaxAccounts = builder.AddParameter("mysync-max-accounts");
var mySyncMaxDebts = builder.AddParameter("mysync-max-debts");
var mySyncTimeZone = builder.AddParameter("mysync-time-zone");
var mySyncMigrateOnStartup = builder.AddParameter("mysync-migrate-on-startup");
// UC13: catalog of currencies and exchange rates (Frankfurter v2, no key).
var mySyncRatesUrl = builder.AddParameter("mysync-rates-url");

var mySyncBackend = builder.AddProject<Projects.FinHorizon_MySync_Backend>("mysync-backend")
    .WithReference(mySyncDb)
    .WaitFor(mySyncDb)
    .WithEnvironment("MySync__MaxAccounts", mySyncMaxAccounts)
    .WithEnvironment("MySync__MaxDebts", mySyncMaxDebts)
    .WithEnvironment("Calendar__TimeZone", mySyncTimeZone)
    .WithEnvironment("Database__MigrateOnStartup", mySyncMigrateOnStartup)
    .WithEnvironment("Currencies__Url", mySyncRatesUrl);

// Fixed port: it must match the redirect URIs and web origins of the Keycloak client mysync-frontend.
builder.AddViteApp("mysync-frontend", "../MySync/FinHorizon.MySync.Frontend")
    .WithEndpoint("http", endpoint => endpoint.Port = 5174)
    .WithEnvironment("BACKEND_URL", mySyncBackend.GetEndpoint("https"))
    .WithExternalHttpEndpoints()
    .WaitFor(mySyncBackend);

builder.Build().Run();
