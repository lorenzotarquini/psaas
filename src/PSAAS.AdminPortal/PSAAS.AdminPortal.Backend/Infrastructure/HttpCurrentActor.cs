using PSAAS.Application.Abstractions.Audit;

namespace PSAAS.AdminPortal.Backend.Infrastructure;

/// <summary>
/// The administrator of the current request, from the Keycloak token; outside a request (scheduled jobs)
/// the actor is the system.
/// </summary>
public sealed class HttpCurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    public const string SystemId = "system";

    public string Id => accessor.HttpContext?.User.FindFirst("sub")?.Value ?? SystemId;

    public string Name
    {
        get
        {
            var user = accessor.HttpContext?.User;
            if (user is null)
            {
                return "Sistema";
            }

            var given = user.FindFirst("given_name")?.Value;
            var family = user.FindFirst("family_name")?.Value;
            return given is not null && family is not null
                ? $"{given} {family}"
                : user.FindFirst("name")?.Value ?? user.Identity?.Name ?? Id;
        }
    }
}
