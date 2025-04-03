﻿using System.Collections;

namespace Rin.Engine.Math;

public class ParamsEnumerator<T>(params T[] data) : IEnumerator<T>
{
    private int _index;

    public void Dispose()
    {
        // TODO release managed resources here
    }

    public bool MoveNext()
    {
        return ++_index < data.Length;
    }

    public void Reset()
    {
        _index = 0;
    }

    public T Current => data[_index];

    object IEnumerator.Current => Current ?? throw new InvalidOperationException();
}