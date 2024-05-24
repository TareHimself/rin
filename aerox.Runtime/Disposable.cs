namespace aerox.Runtime;

public abstract class Disposable : IDisposable
{
    public bool Disposed { get; private set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected abstract void OnDispose(bool isManual);

    protected virtual void Dispose(bool disposing)
    {
        if (Disposed) return;
        Disposed = true;
        //Console.WriteLine("Disposing {0}", GetType().Name);
        OnDispose(disposing);
    }

    ~Disposable()
    {
        Dispose(false);
    }
}