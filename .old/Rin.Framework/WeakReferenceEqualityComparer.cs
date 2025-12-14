namespace Rin.Framework;

public class WeakReferenceEqualityComparer<T> : IEqualityComparer<WeakReference<T>> where T : class
{
    private readonly IEqualityComparer<T>? _comparer;

    public WeakReferenceEqualityComparer(IEqualityComparer<T> comparer)
    {
        _comparer = comparer;
    }

    public WeakReferenceEqualityComparer()
    {
        _comparer = null;
    }

    public bool Equals(WeakReference<T>? x, WeakReference<T>? y)
    {
        T? xTarget = null;
        T? yTarget = null;
        x?.TryGetTarget(out xTarget);
        y?.TryGetTarget(out yTarget);
        return _comparer?.Equals(xTarget, yTarget) ?? x == y;
    }

    public int GetHashCode(WeakReference<T> obj)
    {
        obj.TryGetTarget(out var target);
        if (_comparer is not null && target is not null) return _comparer.GetHashCode(target);
        return target?.GetHashCode() ?? 0;
    }
}