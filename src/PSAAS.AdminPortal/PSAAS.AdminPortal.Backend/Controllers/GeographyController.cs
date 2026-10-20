using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PSAAS.Application.Abstractions.Geography;

namespace PSAAS.AdminPortal.Backend.Controllers;

[ApiController]
[Route("api/geography")]
public sealed class GeographyController(IItalianCityProvider cities) : ControllerBase
{
    [HttpGet("cities")]
    public async Task<ActionResult<IReadOnlyList<ItalianCity>>> SearchCities(
        [FromQuery, Required, MinLength(2)] string term,
        CancellationToken cancellationToken)
    {
        // The provider returns null when the external cities source cannot be reached.
        var result = await cities.SearchAsync(term, cancellationToken);
        return result is null ? Problem(statusCode: StatusCodes.Status503ServiceUnavailable) : Ok(result);
    }
}
