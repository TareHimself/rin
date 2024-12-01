namespace rin.Framework.Core;

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

    public Atomic<T> Update(Func<T,T> update)
    {
        lock (_lock)
        {
            _value = update(_value);
        }

        return this;
    }
    
    
    
    public static implicit operator Atomic<T>(T value) => new Atomic<T>(value);
    public static implicit operator T(Atomic<T> atomic) => atomic.Value;
}