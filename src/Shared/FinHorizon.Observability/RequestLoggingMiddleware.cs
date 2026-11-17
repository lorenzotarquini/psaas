using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NLog;

namespace FinHorizon.Observability;

/// <summary>
/// Gives every log line of a request the pseudonymous id of its user (<see cref="LogFields.UserId"/>) and writes one line
/// per request with method, route template, status and duration. It goes after authentication, so the user is known,
/// and before authorization, so refused requests (401, 403) are logged too.
/// </summary>
public sealed class RequestLoggingMiddleware(RequestDelegate next)
{
    private const string LoggerName = "FinHorizon.Http";
    private const string UnmatchedRoute = "unmatched";

    private static readonly Logger Logger = LogManager.GetLogger(LoggerName);

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var userId = context.User.FindFirst("sub")?.Value;
        using var scope = userId is null ? null : ScopeContext.PushProperty(LogFields.UserId, userId);
        var started = Stopwatch.GetTimestamp();
        var failed = false;
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch
        {
            // The exception handler, further out, answers 500: the request is logged as such.
            failed = true;
            throw;
        }
        finally
        {
            var status = failed ? StatusCodes.Status500InternalServerError : context.Response.StatusCode;
            Logger.ForLogEvent(LevelOf(context, status))
                .Message(
                    "HTTP {Method} {Route} responded {StatusCode} in {ElapsedMs} ms",
                    context.Request.Method,
                    RouteOf(context),
                    status,
                    (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds)
                .Property(LogFields.Event, LogEvents.HttpRequest)
                .Log();
        }
    }

    /// <summary>The template of the matched endpoint, with a leading slash; never the actual path.</summary>
    internal static string RouteOf(HttpContext context) =>
        context.GetEndpoint() is RouteEndpoint endpoint && endpoint.RoutePattern.RawText is { } raw
            ? "/" + raw.TrimStart('/')
            : UnmatchedRoute;

    /// <summary>Health probes only at Debug; server errors at Error, client errors at Warn, the rest at Info.</summary>
    private static LogLevel LevelOf(HttpContext context, int status) =>
        context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) && status < StatusCodes.Status500InternalServerError
            ? LogLevel.Debug
            : status >= StatusCodes.Status500InternalServerError
                ? LogLevel.Error
                : status >= StatusCodes.Status400BadRequest ? LogLevel.Warn : LogLevel.Info;
}

public static class RequestLoggingExtensions
{
    /// <summary>Call it right after <c>UseAuthentication</c> and before <c>UseAuthorization</c>.</summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) => app.UseMiddleware<RequestLoggingMiddleware>();
}
