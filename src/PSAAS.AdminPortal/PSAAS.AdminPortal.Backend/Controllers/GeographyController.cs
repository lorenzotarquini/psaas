using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NLog;
using PSAAS.Application.Abstractions.Geography;

namespace PSAAS.AdminPortal.Backend.Controllers;

[ApiController]
[Route("api/geography")]
public sealed class GeographyController(IItalianCityProvider cities) : ControllerBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [HttpGet("cities")]
    public async Task<ActionResult<IReadOnlyList<ItalianCity>>> SearchCities(
        [FromQuery, Required, MinLength(2)] string term,
        CancellationToken cancellationToken)
    {
        // The provider returns null when the external cities source cannot be reached.
        var result = await cities.SearchAsync(term, cancellationToken);
        if (result is null)
        {
            Logger.Warn("City search for {Term} unavailable: the Italian city list cannot be reached", term);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        // Debug: the frontend calls this on every keystroke of the address autocomplete.
        Logger.Debug("City search for {Term} returned {CityCount} cities", term, result.Count);
        return Ok(result);
    }
}
