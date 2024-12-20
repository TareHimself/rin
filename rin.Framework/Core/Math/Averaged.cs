﻿using System.Numerics;

namespace rin.Framework.Core.Math;

public class Averaged<T> where T : IDivisionOperators<T,T,T> , IAdditionOperators<T,T,T>, ISubtractionOperators<T,T,T>
{
    private uint _count = 1;
    private T _total;
    private readonly LinkedList<T> _items = [];
    private readonly uint _maxCount;

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
        dynamic c = _count;
        return _total / (T)c;
    }
    
    public static implicit operator T(Averaged<T> avg) => avg.Get();

    public Averaged(T initial,uint maxCount)
    {
        _maxCount = maxCount;
        _total = initial;
        _items.AddFirst(initial);
    }
}