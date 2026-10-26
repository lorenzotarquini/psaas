using Microsoft.AspNetCore.Mvc;
using PSAAS.Application.Abstractions.Communications;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Communications;
using PSAAS.Application.Services.Communications;

namespace PSAAS.AdminPortal.Backend.Controllers;

// UC27 (EXP-11): communications to segments of tenants.
[ApiController]
[Route("api/communications")]
public sealed class CommunicationsController(ICommunicationRepository communications, CommunicationService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<Communication>>> GetPage(
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1 || pageSize is < 1 or > 200)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { [page < 1 ? "page" : "pageSize"] = ["InvalidValue"] }));
        }

        return await communications.GetPageAsync(tenantId, page, pageSize, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Communication>> Get(Guid id, CancellationToken cancellationToken) =>
        await communications.GetAsync(id, cancellationToken) is { } communication ? communication : NotFound();

    /// <summary>UC27 step 3: how many tenants the segment reaches, before sending.</summary>
    [HttpPost("preview")]
    public async Task<ActionResult<CommunicationPreview>> Preview(CommunicationSegment segment, CancellationToken cancellationToken)
    {
        var (preview, errors) = await service.PreviewAsync(segment, cancellationToken);
        return preview ?? (ActionResult<CommunicationPreview>)Problem(errors);
    }

    [HttpPost]
    public async Task<ActionResult<Communication>> Send(CommunicationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SendAsync(request, cancellationToken);
        return result.Communication is null
            ? Problem(result.Errors)
            : CreatedAtAction(nameof(Get), new { id = result.Communication.Id }, result.Communication);
    }

    private ActionResult Problem(IReadOnlyDictionary<string, string> errors) =>
        ValidationProblem(new ValidationProblemDetails(errors.ToDictionary(e => e.Key, e => new[] { e.Value })));
}

// UC28: the bell and the "Da gestire" queue.
[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(NotificationService notifications) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<Notification>> Get(CancellationToken cancellationToken) => notifications.GetAsync(cancellationToken);
}
