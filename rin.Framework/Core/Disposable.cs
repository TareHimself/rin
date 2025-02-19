using JetBrains.Annotations;

namespace rin.Framework.Core;

public abstract class Disposable : IDisposable
{
    [PublicAPI] public bool Disposed { get; private set; }

    //public string DisposeId { get; } = Guid.NewGuid().ToString();
    //public string DisposableId { get; private set; } = 

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
        OnDispose(disposing);
    }

    ~Disposable()
    {
        Dispose(false);
    }
}