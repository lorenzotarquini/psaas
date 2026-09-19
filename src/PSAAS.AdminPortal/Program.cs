using Microsoft.AspNetCore.HttpOverrides;
using PSAAS.AdminPortal.Components;
using PSAAS.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddKeycloak(builder.Configuration);
builder.Services.AddCascadingAuthenticationState();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedHost
        | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    // The endpoint requires an authenticated user so unauthenticated requests use
    // the PSAAS.Auth OIDC challenge. The Administrator role is enforced by
    // AuthorizeRouteView through the component-level policy, allowing authenticated
    // users without the role to see the in-app access denied message instead of a
    // cookie access-denied redirect to a non-existent placeholder endpoint.
    .RequireAuthorization();

app.Run();
