namespace Rin.World.Mesh.Skinning;

internal sealed class PooledAnimationNotifyStateFactory<T> : IAnimationNotifyStateFactory
    where T : class, IAnimationNotifyState, new()
{
    private readonly Stack<T> _pool = new();

    public IAnimationNotifyState Create()
    {
        return _pool.Count > 0 ? _pool.Pop() : new T();
    }

    public void Release(IAnimationNotifyState instance)
    {
        var typed = (T)instance;
        typed.Reset();
        _pool.Push(typed);
    }
}

public static class NotifyStateFactory<T> where T : class, IAnimationNotifyState, new()
{
    public static readonly IAnimationNotifyStateFactory Instance = new PooledAnimationNotifyStateFactory<T>();
}
