using Microsoft.AspNetCore.Mvc;
using PSAAS.Application.Contracts.Tenants;

namespace PSAAS.AdminPortal.Backend.Contracts;

/// <summary>Query string of the tenant list (UC21): status and planId may repeat, ids is the selection to export.</summary>
public sealed class TenantListParameters
{
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "status")]
    public string[]? Statuses { get; set; }

    [FromQuery(Name = "planId")]
    public Guid[]? PlanIds { get; set; }

    [FromQuery(Name = "moduleId")]
    public string? ModuleId { get; set; }

    [FromQuery(Name = "province")]
    public string? Province { get; set; }

    [FromQuery(Name = "renewalWithinDays")]
    public int? RenewalWithinDays { get; set; }

    [FromQuery(Name = "ids")]
    public Guid[]? Ids { get; set; }

    [FromQuery(Name = "sort")]
    public string? Sort { get; set; }

    public TenantListQuery ToQuery(int page, int pageSize) => new(
        Search, Statuses, PlanIds, ModuleId, Province, RenewalWithinDays, Ids,
        string.IsNullOrWhiteSpace(Sort) ? TenantListSorts.CompanyName : Sort.Trim(), page, pageSize);
}
