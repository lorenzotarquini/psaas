using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Backend.Infrastructure;

/// <summary>The Keycloak subject of the token is the owner of every record (RNF2).</summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public OwnerId Id =>
        new(accessor.HttpContext?.User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("No authenticated user: every MySync endpoint requires one."));
}
