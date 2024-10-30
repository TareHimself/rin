namespace rin.Core;

public abstract class MultiDisposable : Disposable
{
    private readonly Mutex _mutex = new();
    private long _reservations;

    public MultiDisposable Reserve()
    {
        _mutex.WaitOne();
        _reservations++;
        _mutex.ReleaseMutex();
        return this;
    }

    protected override void Dispose(bool disposing)
    {
        _mutex.WaitOne();
        _reservations--;
        if (_reservations < 0) base.Dispose(disposing);
        _mutex.ReleaseMutex();
    }
}