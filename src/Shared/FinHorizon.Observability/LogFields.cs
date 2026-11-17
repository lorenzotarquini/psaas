namespace FinHorizon.Observability;

/// <summary>
/// Names of the structured fields every FinHorizon backend writes, the same in every service: Alloy, Loki and the Grafana
/// dashboards read them. Never personal data: the user is the Keycloak subject, a pseudonymous id, never a name or an email.
/// </summary>
public static class LogFields
{
    /// <summary>What kind of event a line is: one of <see cref="LogEvents"/>.</summary>
    public const string Event = "Event";

    /// <summary>The Keycloak subject (<c>sub</c>) of the signed-in user, on every line written during the request.</summary>
    public const string UserId = "UserId";

    public const string Method = "Method";

    /// <summary>The route template (<c>api/subscriptions/{id:guid}</c>), never the URL: ids and query strings stay out.</summary>
    public const string Route = "Route";

    public const string StatusCode = "StatusCode";

    public const string ElapsedMs = "ElapsedMs";
}

/// <summary>The values of <see cref="LogFields.Event"/>.</summary>
public static class LogEvents
{
    /// <summary>One line per HTTP request, when it ends.</summary>
    public const string HttpRequest = "http_request";

    /// <summary>One line per use case, with its outcome (MySync).</summary>
    public const string UseCase = "use_case";
}
