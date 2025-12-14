namespace Rin.Framework;

public class BlockingStack<T>
{
    private readonly object _lock = new();
    private readonly object _popLock = new();
    private readonly Stack<T> _stack = new();
    private readonly AutoResetEvent _waitEvent = new(false);

    public void Push(T item)
    {
        lock (_lock)
        {
            _stack.Push(item);
            _waitEvent.Set();
        }
    }

    public T Pop()
    {
        lock (_popLock)
        {
            var sucess = false;

            lock (_lock)
            {
                sucess = _stack.Capacity > 0;
            }

            if (!sucess) _waitEvent.WaitOne();

            lock (_stack)
            {
                return _stack.Pop();
            }
        }
    }
}