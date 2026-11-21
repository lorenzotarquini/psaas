using FinHorizon.Modules.Sdk;
using FinHorizon.MySync.Application.Features.Memberships;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinHorizon.MySync.Backend.Controllers;

/// <summary>
/// The manifest of MySync as an implementation of the module contract (docs/architettura-moduli.md): public, so the
/// administrator can register it (or a new version of its tiers) in the Admin Portal catalog (UC15).
/// </summary>
[ApiController]
[Route("api/module")]
public sealed class ModuleController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("manifest")]
    public ModuleManifest Manifest() =>
        MySyncModule.Manifest(typeof(ModuleController).Assembly.GetName().Version?.ToString(3) ?? "1.0.0");
}
