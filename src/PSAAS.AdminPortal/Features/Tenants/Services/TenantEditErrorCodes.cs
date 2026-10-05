namespace PSAAS.AdminPortal.Features.Tenants.Services;

/// <summary>
/// Error codes for tenant edit failures that are not tied to a single field.
/// </summary>
public static class TenantEditErrorCodes
{
    public const string NotFound = "NotFound";

    public const string SaveFailed = "SaveFailed";
}
