namespace aerox.Runtime;

public class AsyncDisposable : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}