using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Domain;
using PSAAS.MVVM;

namespace PSAAS.AdminPortal.Features.Tenants.ViewModels;

public sealed class TenantGridViewModel(ITenantReadRepository tenantReadRepository) : InjectedViewModel
{
    private IReadOnlyList<Tenant> _tenants = Array.Empty<Tenant>();
    private bool _isLoading;

    public IReadOnlyList<Tenant> Tenants
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
            Tenants = await tenantReadRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
