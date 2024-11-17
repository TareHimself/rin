namespace rin.Core;

public class Atomic<T>(T value)
{
    private readonly object _lock = new();
    private T _value = value;

    public T Value
    {
        get
        {
            lock (_lock)
            {
                return _value;
            }
        }
        set
        {
            lock (_lock)
            {
                _value = value;
            }
        }
    }
    
    public static implicit operator Atomic<T>(T value) => new Atomic<T>(value);
    public static implicit operator T(Atomic<T> atomic) => atomic.Value;
}