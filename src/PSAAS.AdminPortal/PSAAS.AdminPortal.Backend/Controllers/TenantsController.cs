using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts;
using PSAAS.Domain;

namespace PSAAS.AdminPortal.Backend.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController(ITenantReadRepository tenants) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<TenantSummaryDto>> GetPage(
        [FromQuery] string? search,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await tenants.GetPageAsync(search, page, pageSize, cancellationToken);
        return new PagedResult<TenantSummaryDto>(
            result.Items.Select(TenantSummaryDto.From).ToArray(),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await tenants.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return NotFound();
        }

        var structure = await tenants.GetProfilesAndProductiveUnitsAsync(id.ToString(), cancellationToken);
        return new TenantDetailDto(
            tenant.Id,
            tenant.CompanyName,
            tenant.VatNumber,
            tenant.CompleteVatNumber,
            tenant.CertifiedEmail,
            AddressDto.From(tenant.Address),
            structure.Profiles,
            structure.ProductiveUnits);
    }
}

public sealed record TenantSummaryDto(
    Guid Id,
    string CompanyName,
    string VatNumber,
    string CompleteVatNumber,
    string CertifiedEmail,
    string City)
{
    public static TenantSummaryDto From(Tenant tenant) => new(
        tenant.Id,
        tenant.CompanyName,
        tenant.VatNumber,
        tenant.CompleteVatNumber,
        tenant.CertifiedEmail,
        tenant.Address.city);
}

public sealed record TenantDetailDto(
    Guid Id,
    string CompanyName,
    string VatNumber,
    string CompleteVatNumber,
    string CertifiedEmail,
    AddressDto Address,
    IReadOnlyList<TenantProfileReadModel> Profiles,
    IReadOnlyList<TenantProductiveUnitReadModel> ProductiveUnits);

public sealed record AddressDto(string Street, string City, string PostalCode, string Country)
{
    public static AddressDto From(Address address) =>
        new(address.street, address.city, address.postalCode, address.country);
}
