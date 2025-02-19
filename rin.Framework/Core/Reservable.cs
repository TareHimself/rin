namespace rin.Framework.Core;

public abstract class Reservable : Disposable, IReservable
{
    private readonly object _lock = new();
    private long _reservations;

    public void Reserve()
    {
        lock (_lock)
        {
            _reservations++;
        }
    }

    protected override void Dispose(bool disposing)
    {
        lock (_lock)
        {
            _reservations--;
            if (_reservations < 0) base.Dispose(disposing);
        }
    }
}