namespace Rin.Framework;

public class AsyncDisposable : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}