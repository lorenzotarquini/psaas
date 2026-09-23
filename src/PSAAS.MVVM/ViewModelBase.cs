using CommunityToolkit.Mvvm.ComponentModel;

namespace PSAAS.MVVM;

/// <summary>
/// Base type for view models with explicit initialization and deterministic disposal support.
/// </summary>
public abstract class ViewModelBase : ObservableObject, IDisposable, IAsyncDisposable
{
    private readonly CancellationTokenSource _disposeCancellation = new();
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private bool _isDisposed;
    private bool _isInitialized;
    private bool _isInitializing;
    private int _disposeStarted;

    /// <summary>
    /// Gets a value indicating whether the view model completed initialization successfully.
    /// </summary>
    public bool IsInitialized
    {
        get => _isInitialized;
        private set => SetProperty(ref _isInitialized, value);
    }

    /// <summary>
    /// Gets a value indicating whether initialization is currently running.
    /// </summary>
    public bool IsInitializing
    {
        get => _isInitializing;
        private set => SetProperty(ref _isInitializing, value);
    }

    /// <summary>
    /// Gets a value indicating whether disposal has started.
    /// </summary>
    public bool IsDisposed
    {
        get => _isDisposed;
        private set => SetProperty(ref _isDisposed, value);
    }

    /// <summary>
    /// Gets a token cancelled when disposal starts.
    /// </summary>
    protected CancellationToken DisposalToken => _disposeCancellation.Token;

    /// <summary>
    /// Explicitly initializes the view model. Multiple successful calls are idempotent.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel initialization.</param>
    /// <returns>A task representing the initialization operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        ThrowIfDisposed();

        using var waitCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, DisposalToken);
        await _lifecycleLock.WaitAsync(waitCancellation.Token).ConfigureAwait(false);

        try
        {
            if (IsInitialized)
            {
                return;
            }

            ThrowIfDisposed();

            using var initializeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, DisposalToken);
            IsInitializing = true;

            try
            {
                await OnInitializeAsync(initializeCancellation.Token).ConfigureAwait(false);
                initializeCancellation.Token.ThrowIfCancellationRequested();
                IsInitialized = true;
            }
            finally
            {
                IsInitializing = false;
            }
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <summary>
    /// Performs view-model specific initialization.
    /// </summary>
    /// <param name="cancellationToken">Token cancelled when initialization is cancelled or disposal starts.</param>
    /// <returns>A task representing the initialization operation.</returns>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }

        IsDisposed = true;
        _disposeCancellation.Cancel();
        _lifecycleLock.Wait();

        try
        {
            DisposeManagedResources();
        }
        finally
        {
            _disposeCancellation.Dispose();
            _lifecycleLock.Release();
            _lifecycleLock.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }

        IsDisposed = true;
        await _disposeCancellation.CancelAsync().ConfigureAwait(false);
        await _lifecycleLock.WaitAsync().ConfigureAwait(false);

        try
        {
            await DisposeManagedResourcesAsync().ConfigureAwait(false);
            DisposeManagedResources();
        }
        finally
        {
            _disposeCancellation.Dispose();
            _lifecycleLock.Release();
            _lifecycleLock.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Releases synchronous resources owned by the view model.
    /// </summary>
    protected virtual void DisposeManagedResources()
    {
    }

    /// <summary>
    /// Releases asynchronous resources owned by the view model.
    /// </summary>
    /// <returns>A value task representing the asynchronous disposal operation.</returns>
    protected virtual ValueTask DisposeManagedResourcesAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Throws when the view model has been disposed.
    /// </summary>
    protected void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);
}
