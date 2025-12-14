namespace Rin.Framework.Shared.Pooling;

public interface IPooledObject
{
    /// <summary>
    ///     Called when this <see cref="IPooledObject" /> is released
    /// </summary>
    public void Reset(IObjectPool pool);
}