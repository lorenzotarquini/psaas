using PSAAS.MVVM;
using Xunit;

namespace PSAAS.MVVM.Tests;

public sealed class ViewModelBaseTests
{
    [Fact]
    public async Task InitializeAsync_WhenCalledMultipleTimes_InitializesOnce()
    {
        var viewModel = new TestViewModel();

        await viewModel.InitializeAsync();
        await viewModel.InitializeAsync();

        Assert.True(viewModel.IsInitialized);
        Assert.False(viewModel.IsInitializing);
        Assert.Equal(1, viewModel.InitializeCount);
    }

    [Fact]
    public async Task InitializeAsync_WhenCancelled_DoesNotMarkInitialized()
    {
        var viewModel = new TestViewModel();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => viewModel.InitializeAsync(cancellationTokenSource.Token));

        Assert.False(viewModel.IsInitialized);
        Assert.False(viewModel.IsInitializing);
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_DisposesOnce()
    {
        var viewModel = new TestViewModel();

        viewModel.Dispose();
        viewModel.Dispose();

        Assert.True(viewModel.IsDisposed);
        Assert.Equal(1, viewModel.DisposeCount);
    }

    [Fact]
    public async Task DisposeAsync_DisposesAsyncAndSyncResourcesOnce()
    {
        var viewModel = new TestViewModel();

        await viewModel.DisposeAsync();
        await viewModel.DisposeAsync();

        viewModel.Dispose();

        Assert.True(viewModel.IsDisposed);
        Assert.Equal(1, viewModel.AsyncDisposeCount);
        Assert.Equal(1, viewModel.DisposeCount);
    }

    private sealed class TestViewModel : ViewModelBase
    {
        public int InitializeCount { get; private set; }

        public int DisposeCount { get; private set; }

        public int AsyncDisposeCount { get; private set; }

        protected override Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            InitializeCount++;
            return Task.CompletedTask;
        }

        protected override void DisposeManagedResources()
        {
            DisposeCount++;
        }

        protected override ValueTask DisposeManagedResourcesAsync()
        {
            AsyncDisposeCount++;
            return ValueTask.CompletedTask;
        }
    }
}
