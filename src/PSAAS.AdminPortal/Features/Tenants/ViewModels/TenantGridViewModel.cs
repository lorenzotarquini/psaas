using PSAAS.AdminPortal.Features.Tenants.Services;
using PSAAS.MVVM;

namespace PSAAS.AdminPortal.Features.Tenants.ViewModels;

public sealed class TenantGridViewModel(ITenantService tenantService) : InjectedViewModel
{
    private IReadOnlyList<TenantListItem> _tenants = Array.Empty<TenantListItem>();
    private bool _isLoading;

    public IReadOnlyList<TenantListItem> Tenants
    {
        get => _tenants;
        private set => SetProperty(ref _tenants, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IsLoading = true;

        try
        {
            Tenants = await tenantService.GetTenantListAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
