namespace Rin.World.Mesh.Skinning;

internal sealed class PooledAnimationNotifyFactory<T> : IAnimationNotifyFactory where T : class, IAnimationNotify, new()
{
    private readonly Stack<T> _pool = new();

    public IAnimationNotify Create()
    {
        return _pool.Count > 0 ? _pool.Pop() : new T();
    }

    public void Release(IAnimationNotify instance)
    {
        var typed = (T)instance;
        typed.Reset();
        _pool.Push(typed);
    }
}

public static class NotifyFactory<T> where T : class, IAnimationNotify, new()
{
    public static readonly IAnimationNotifyFactory Instance = new PooledAnimationNotifyFactory<T>();
}
