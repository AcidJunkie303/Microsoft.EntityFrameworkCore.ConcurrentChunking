using ConcurrentChunking.Testing.Support;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;

public abstract class TestData : IDisposable
{
    private readonly AsyncLock _initializationLock = new();
    private bool _isInitialized;

    public async Task EnsureInitializedAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        using var releaser = await _initializationLock.LockAsync();

        if (_isInitialized)
        {
            return;
        }

        await InitializeAsync();
        _isInitialized = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected abstract Task InitializeAsync();

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _initializationLock.Dispose();
        }
    }
}
