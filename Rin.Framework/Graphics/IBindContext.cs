namespace Rin.Framework.Graphics;

/// <summary>
///     Context created after a shader has been bound
/// </summary>
public interface IBindContext
{
    //public IBindContext WriteImage(string name, IDeviceImage image);
}

public interface IBindContext<out TInterface> : IBindContext where TInterface : IBindContext
{
    /// <summary>
    ///     Resets the descriptor state of this bind context i.e. descriptors are forgotten and subsequent draws will allocate
    ///     new descriptors
    /// </summary>
    public TInterface Reset();

    public TInterface Push<T>(in T data, uint offset = 0) where T : unmanaged;

    public TInterface WriteBuffer(string name, in DeviceBufferView view, uint arrayOffset = 0);
}