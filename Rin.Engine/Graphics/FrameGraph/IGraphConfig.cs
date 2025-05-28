namespace Rin.Engine.Graphics.FrameGraph;

public interface IGraphConfig
{
    public uint SwapchainImageId { get; }

    public uint AddExternalImage(IDeviceImage image, Action? onDispose = null);

    public uint CreateImage(uint width, uint height, ImageFormat format,
        ImageLayout layout);

    public uint CreateImage(in Extent2D extent, ImageFormat format,
        ImageLayout layout);

    public uint CreateImage(in Extent3D extent, ImageFormat format,
        ImageLayout layout);

    public uint CreateBuffer(ulong size, GraphBufferUsage usage);

    public uint CreateBuffer<T>(GraphBufferUsage usage) where T : unmanaged
    {
        return CreateBuffer(Utils.ByteSizeOf<T>(), usage);
    }

    public uint CreateBuffer<T>(int count, GraphBufferUsage usage) where T : unmanaged
    {
        return CreateBuffer(Utils.ByteSizeOf<T>(count), usage);
    }

    public uint CreateBuffer<T>(uint count, GraphBufferUsage usage) where T : unmanaged
    {
        return CreateBuffer(Utils.ByteSizeOf<T>() * count, usage);
    }
    
    public uint UseImage(uint id, ImageLayout layout, ResourceOperation operation);
    public uint UseBuffer(uint id, GraphBufferUsage usage, ResourceOperation operation);

    public uint ReadBuffer(uint id, GraphBufferUsage usage)
    {
        return UseBuffer(id, usage, ResourceOperation.Read);
    }

    public uint WriteBuffer(uint id, GraphBufferUsage usage)
    {
        return UseBuffer(id, usage, ResourceOperation.Write);
    }

    public uint ReadImage(uint id, ImageLayout layout)
    {
        return UseImage(id, layout, ResourceOperation.Read);
    }

    public uint WriteImage(uint id, ImageLayout layout)
    {
        return UseImage(id, layout, ResourceOperation.Write);
    }

    /// <summary>
    ///     Adds an explicit dependency to the specified pass ID (NOTE. this may not be what you need)
    /// </summary>
    /// <param name="passId"></param>
    /// <returns></returns>
    public uint DependOn(uint passId);
}