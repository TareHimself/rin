namespace Rin.Engine.Curves;

public abstract class Curve<TValue,TInterpolatedValue> : ICurve<TInterpolatedValue> where TValue : struct where TInterpolatedValue : struct 
{
    
    
    private readonly SortedList<float, TValue> _points = [];

    protected abstract TInterpolatedValue Interpolate(in TValue previous, in TValue next, float alpha);
    
    protected abstract TInterpolatedValue ToInterpolatedValue(in TValue value);

    private int NearestIndex(float time)
    {
        switch (_points.Count)
        {
            case 0:
                return -1;
            case 1:
                return 0;
        }

        var minIdx = 0;
        var maxIdx = _points.Count - 1;
        var currentIdx = (int)float.Floor(maxIdx / 2.0f);
        var totalRange = _points.Count;
        while (true)
        {
            if (totalRange % 2 == 0)
            {
                var distToCurrent = System.Math.Abs(time - _points.Keys[currentIdx]);
                var distToNext = System.Math.Abs(time - _points.Keys[currentIdx + 1]);
                if (distToNext < distToCurrent) currentIdx++;
            }

            if (time < _points.Keys[currentIdx])
            {
                if (maxIdx == currentIdx) return currentIdx;
                maxIdx = currentIdx;
            }
            else
            {
                if (minIdx == currentIdx) return currentIdx;
                minIdx = currentIdx;
            }

            currentIdx = maxIdx + (maxIdx - minIdx) / 2;
            totalRange = maxIdx - minIdx + 1;
        }
    }

    public void Add(float time, in TValue data)
    {
        _points.Add(time, data);
    }

    public void Remove(float time)
    {
        _points.Remove(time);
    }

    public void RemoveAt(int index)
    {
        _points.RemoveAt(index);
    }

    public IEnumerable<KeyValuePair<float,TValue>> GetPoints() => _points;
    
    public TInterpolatedValue Evaluate(float time)
    {
        if (_points.Count < 1) throw new IndexOutOfRangeException();
        if (_points.Count == 1) return ToInterpolatedValue(_points[_points.Keys.First()]);

        var minKey = _points.Keys.Min();
        if (time < minKey) return ToInterpolatedValue(_points.First().Value);
        var maxKey = _points.Keys.Last();
        if (time > maxKey) return ToInterpolatedValue(_points.Last().Value);

        var idx = NearestIndex(time);
        if (idx < 0) throw new Exception("Failed to evaluate curve");
        var nearest = _points.Keys[idx];
        TValue previousValue;
        TValue nextValue;
        float previousTime;
        float nextTime;
        if (nearest <= time)
        {
            var nextIdx = (idx + 1) % _points.Count;
            previousTime = _points.Keys[idx];
            nextTime = _points.Keys[nextIdx];
            previousValue = _points.Values[idx];
            nextValue = _points.Values[nextIdx];
        }
        else
        {
            var previousIdx = idx - 1;
            if (previousIdx < 0) previousIdx = _points.Count - int.Abs(idx);
            previousTime = _points.Keys[previousIdx];
            nextTime = _points.Keys[idx];
            previousValue = _points.Values[previousIdx];
            nextValue = _points.Values[idx];
        }

        var dist = nextTime - previousTime;
        var alpha = (time - previousTime) / dist;
        return Interpolate(previousValue, nextValue, alpha);
    }
}