using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var postgresUsername = builder.AddParameter("postgres-username");
var postgresPassword = builder.AddParameter("postgres-password");

var postgres = builder.AddPostgres("postgres", postgresUsername, postgresPassword, port: 5432)
    .WithDataBindMount("../postgres_data");

var adminPortalDb = postgres.AddDatabase("adminportal-db", "adminportal-db");
var mySyncDb = postgres.AddDatabase("mysync-db", "mysync-db");

// Logs: every backend writes JSON lines to its own file in this directory (NLog target jsonFile, Development),
// Alloy reads them and sends them to Loki, Grafana shows them in one dashboard per service. Configuration in Observability/.
var logsDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../logs"));
Directory.CreateDirectory(logsDirectory);

var loki = builder.AddContainer("loki", "grafana/loki", "3.5.3")
    .WithBindMount("Observability/loki/loki.yaml", "/etc/loki/loki.yaml", isReadOnly: true)
    .WithVolume("finhorizon-loki-data", "/loki")
    .WithArgs("-config.file=/etc/loki/loki.yaml")
    .WithHttpEndpoint(targetPort: 3100, name: "http")
    .WithHttpHealthCheck("/ready");

builder.AddContainer("alloy", "grafana/alloy", "v1.10.2")
    .WithBindMount("Observability/alloy/config.alloy", "/etc/alloy/config.alloy", isReadOnly: true)
    .WithBindMount(logsDirectory, "/var/log/finhorizon", isReadOnly: true)
    // Where Alloy remembers how far it read each file, so a restart sends nothing twice.
    .WithVolume("finhorizon-alloy-data", "/var/lib/alloy/data")
    .WithArgs("run", "--server.http.listen-addr=0.0.0.0:12345", "--storage.path=/var/lib/alloy/data", "/etc/alloy/config.alloy")
    .WithHttpEndpoint(targetPort: 12345, name: "http")
    .WaitFor(loki);

// Fixed port and anonymous access: local development only, nothing to sign in to. Viewers may use Explore;
// the dashboards are provisioned from files and cannot be changed from the UI.
builder.AddContainer("grafana", "grafana/grafana", "12.1.1")
    .WithBindMount("Observability/grafana/provisioning", "/etc/grafana/provisioning", isReadOnly: true)
    .WithBindMount("Observability/grafana/dashboards", "/var/lib/grafana/dashboards", isReadOnly: true)
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Viewer")
    .WithEnvironment("GF_USERS_VIEWERS_CAN_EDIT", "true")
    .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true")
    .WithEnvironment("GF_ANALYTICS_REPORTING_ENABLED", "false")
    .WithEnvironment("GF_DASHBOARDS_DEFAULT_HOME_DASHBOARD_PATH", "/var/lib/grafana/dashboards/finhorizon-overview.json")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http")
    .WithExternalHttpEndpoints()
    .WaitFor(loki);

// Secret of the confidential client adminportal-api: its service account manages users, reads login events
// and exchanges tokens for impersonation. Set it with: dotnet user-secrets set "Parameters:admin-portal-client-secret" "<value>"
var keycloakClientSecret = builder.AddParameter("admin-portal-client-secret", secret: true);

var adminPortalBackend = builder.AddProject<Projects.FinHorizon_AdminPortal_Backend>("adminportal-backend")
    .WithEnvironment("Logs__Directory", logsDirectory)
    .WithReference(adminPortalDb)
    .WaitFor(adminPortalDb)
    .WithEnvironment("Keycloak__ClientSecret", keycloakClientSecret)
    // Locally emails are written as .eml files instead of being sent: no mail reaches the clients' PECs.
    .WithEnvironment("Email__From", "noreply@finhorizon.local")
    .WithEnvironment("Email__PickupDirectory", Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../mail_pickup")));

// Stripe sandbox (docs/admin-portal/STRIPE.md): the secret key (sk_test_…) and the signing secret of the Stripe CLI
// (whsec_…), both in appsettings.Development.json or user secrets, never in git.
var stripeSecretKey = builder.AddParameter("stripe-secret-key", secret: true);
var stripeWebhookSecret = builder.AddParameter("stripe-webhook-secret", secret: true);
adminPortalBackend.WithEnvironment("Stripe__WebhookSecret", stripeWebhookSecret);

// UC33 locally: the Stripe CLI receives the events of the sandbox and forwards them to the backend webhook.
// --skip-verify because the backend uses the development certificate.
builder.AddContainer("stripe-cli", "stripe/stripe-cli", "v1.55.6")
    .WithEnvironment("STRIPE_API_KEY", stripeSecretKey)
    .WithArgs(context =>
    {
        context.Args.Add("listen");
        context.Args.Add("--forward-to");
        context.Args.Add(ReferenceExpression.Create($"{adminPortalBackend.GetEndpoint("https")}/api/stripe/webhook"));
        context.Args.Add("--skip-verify");
    })
    .WaitFor(adminPortalBackend);

// Fixed port: it must match the redirect URIs and web origins of the Keycloak client adminportal-frontend.
builder.AddViteApp("adminportal-frontend", "../AdminPortal/FinHorizon.AdminPortal.Frontend")
    .WithEndpoint("http", endpoint => endpoint.Port = 5173)
    .WithEnvironment("BACKEND_URL", adminPortalBackend.GetEndpoint("https"))
    .WithExternalHttpEndpoints()
    .WaitFor(adminPortalBackend);

// MySync: its own database, backend and frontend; users sign in with the same realm.
// MySync settings, all here (Parameters in appsettings.json): accounts and debts per user (UC2-R6, UC3-R5),
// and the time zone that decides "today".
var mySyncMaxAccounts = builder.AddParameter("mysync-max-accounts");
var mySyncMaxDebts = builder.AddParameter("mysync-max-debts");
var mySyncMaxSubscriptions = builder.AddParameter("mysync-max-subscriptions");
var mySyncTimeZone = builder.AddParameter("mysync-time-zone");
// UC13: catalog of currencies and exchange rates (Frankfurter v2, no key).
var mySyncRatesUrl = builder.AddParameter("mysync-rates-url");

var mySyncBackend = builder.AddProject<Projects.FinHorizon_MySync_Backend>("mysync-backend")
    .WithEnvironment("Logs__Directory", logsDirectory)
    .WithReference(mySyncDb)
    .WaitFor(mySyncDb)
    .WithEnvironment("MySync__MaxAccounts", mySyncMaxAccounts)
    .WithEnvironment("MySync__MaxDebts", mySyncMaxDebts)
    .WithEnvironment("MySync__MaxSubscriptions", mySyncMaxSubscriptions)
    .WithEnvironment("Calendar__TimeZone", mySyncTimeZone)
    .WithEnvironment("Currencies__Url", mySyncRatesUrl);

// UC17 of MySync: the subscription of each user to MySync is paid on Stripe (same sandbox). MySync keeps its own copy
// of the state, so it has its own Stripe CLI forwarding the events to its webhook; Stripe returns to the frontend.
var mySyncMembershipPrice = builder.AddParameter("mysync-membership-price");
mySyncBackend
    .WithEnvironment("Membership__Price", mySyncMembershipPrice)
    .WithEnvironment("Stripe__SecretKey", stripeSecretKey)
    .WithEnvironment("Stripe__WebhookSecret", stripeWebhookSecret)
    .WithEnvironment("Membership__FrontendUrl", "http://localhost:5174");

builder.AddContainer("stripe-cli-mysync", "stripe/stripe-cli", "v1.55.6")
    .WithEnvironment("STRIPE_API_KEY", stripeSecretKey)
    .WithArgs(context =>
    {
        context.Args.Add("listen");
        context.Args.Add("--forward-to");
        context.Args.Add(ReferenceExpression.Create($"{mySyncBackend.GetEndpoint("https")}/api/membership/webhook"));
        context.Args.Add("--skip-verify");
    })
    .WaitFor(mySyncBackend);

// Fixed port: it must match the redirect URIs and web origins of the Keycloak client mysync-frontend.
builder.AddViteApp("mysync-frontend", "../MySync/FinHorizon.MySync.Frontend")
    .WithEndpoint("http", endpoint => endpoint.Port = 5174)
    .WithEnvironment("BACKEND_URL", mySyncBackend.GetEndpoint("https"))
    .WithExternalHttpEndpoints()
    .WaitFor(mySyncBackend);

builder.Build().Run();
