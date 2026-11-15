using FinHorizon.MySync.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FinHorizon.MySync.Backend.Infrastructure;

/// <summary>One mapping from use-case outcomes to HTTP, the same for every endpoint (RNF2, RNF5).</summary>
internal static class ResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(controller);
        return result.Status switch
        {
            ResultStatus.Ok => controller.Ok(result.Value),
            ResultStatus.Created => controller.StatusCode(StatusCodes.Status201Created, result.Value),
            ResultStatus.NotFound => controller.NotFound(),
            ResultStatus.Invalid => controller.ValidationProblem(new ValidationProblemDetails(
                result.Errors.ToDictionary(error => error.Field, error => new[] { error.Code }))),
            _ => throw new InvalidOperationException($"Unknown result status {result.Status}.")
        };
    }

    public static IActionResult ToNoContent(this Result<Unit> result, ControllerBase controller)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(controller);
        return result.Status == ResultStatus.Ok ? controller.NoContent() : result.ToActionResult(controller).Result!;
    }
}
