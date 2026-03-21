using System.Numerics;
using System.Numerics;
namespace Rin.Framework.Shared.Math;

public class Averaged<T>
    // where T : IDivisionOperators<T, T, T>, IAdditionOperators<T, T, T>, ISubtractionOperators<T, T, T>
    where T : INumber<T>
{
    private readonly LinkedList<T> _items = [];
    private readonly uint _maxCount;
    private uint _count = 1;
    private T _total;

    public Averaged(T initial, uint maxCount)
    {
        _maxCount = maxCount;
        _total = initial;
        _items.AddFirst(initial);
    }

    public Averaged<T> Add(T item)
    {
        if (_count == _maxCount)
        {
            _total -= _items.Last();
            _items.RemoveLast();
        }
        else
        {
            _count++;
        }

        _total += item;

        _items.AddFirst(item);
        return this;
    }

    public T Get()
    {
        return _total / T.CreateChecked(_count);
    }

    public static implicit operator T(Averaged<T> avg)
    {
        return avg.Get();
    }
}