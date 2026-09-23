using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using PSAAS.MVVM;

namespace PSAAS.AdminPortal.Components;

/// <summary>
/// Blazor bridge for dependency-injected view models.
/// </summary>
/// <typeparam name="TViewModel">The concrete view model owned by the component instance.</typeparam>
public abstract class MvvmComponentBase<TViewModel> : ComponentBase, IAsyncDisposable
    where TViewModel : ViewModelBase
{
    private bool _disposed;

    /// <summary>
    /// Gets the view model resolved by Blazor dependency injection.
    /// </summary>
    [Inject]
    protected TViewModel ViewModel { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        await ViewModel.InitializeAsync();
    }

    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            await ViewModel.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }
}
