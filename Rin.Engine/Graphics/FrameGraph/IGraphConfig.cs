namespace Rin.Engine.Graphics.FrameGraph;

public interface IGraphConfig
{
    public uint SwapchainImageId { get; }

    public uint AddExternalImage(IDeviceImage image,Action? onDispose = null);
    
    public uint CreateImage(uint width, uint height, ImageFormat format,
        ImageLayout initialLayout);

    public uint CreateImage(in Extent2D extent, ImageFormat format,
        ImageLayout initialLayout);

    public uint CreateBuffer(ulong size, BufferStage stage);

    public uint CreateBuffer<T>(BufferStage stage) where T : unmanaged
    {
        return CreateBuffer(Engine.Utils.ByteSizeOf<T>(), stage);
    }

    public uint CreateBuffer<T>(int count, BufferStage stage) where T : unmanaged
    {
        return CreateBuffer(Engine.Utils.ByteSizeOf<T>(count), stage);
    }

    public uint CreateBuffer<T>(uint count, BufferStage stage) where T : unmanaged
    {
        return CreateBuffer(Engine.Utils.ByteSizeOf<T>() * count, stage);
    }

    // public uint Read(uint resourceId);
    // public uint Write(uint resourceId);
    //
    public uint UseImage(uint id, ImageLayout layout, ResourceUsage usage);
    public uint UseBuffer(uint id, BufferStage stage, ResourceUsage usage);

    public uint ReadBuffer(uint id, BufferStage stage)
    {
        return UseBuffer(id, stage, ResourceUsage.Read);
    }

    public uint WriteBuffer(uint id, BufferStage stage)
    {
        return UseBuffer(id, stage, ResourceUsage.Write);
    }

    public uint ReadImage(uint id, ImageLayout layout)
    {
        return UseImage(id, layout, ResourceUsage.Read);
    }

    public uint WriteImage(uint id, ImageLayout layout)
    {
        return UseImage(id, layout, ResourceUsage.Write);
    }

    /// <summary>
    ///     Adds an explicit dependency to the specified pass ID (NOTE. this may not be what you need)
    /// </summary>
    /// <param name="passId"></param>
    /// <returns></returns>
    public uint DependOn(uint passId);
}