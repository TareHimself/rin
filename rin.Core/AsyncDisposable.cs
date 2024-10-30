namespace rin.Core;

public class AsyncDisposable : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}