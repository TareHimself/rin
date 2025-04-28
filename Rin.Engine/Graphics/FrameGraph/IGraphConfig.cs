namespace Rin.Engine.Graphics.FrameGraph;

public interface IGraphConfig
{
    public uint CreateImage(uint width, uint height, ImageFormat format,
        ImageLayout initialLayout = ImageLayout.Undefined);
    
    public uint CreateImage(in Extent2D extent, ImageFormat format,
        ImageLayout initialLayout = ImageLayout.Undefined);

    public uint AllocateBuffer(ulong size);
    // public uint Read(uint resourceId);
    // public uint Write(uint resourceId);
    //
    public uint UseImage(uint id,ImageLayout layout,ResourceUsage usage);
    public uint UseBuffer(uint id,BufferStage stage,ResourceUsage usage);

    public uint DependOn(uint passId);

    public uint AllocateBuffer<T>() where T : unmanaged
    {
        return AllocateBuffer(Engine.Utils.ByteSizeOf<T>());
    }

    public uint AllocateBuffer<T>(int count) where T : unmanaged
    {
        return AllocateBuffer(Engine.Utils.ByteSizeOf<T>(count));
    }
    
    public uint AllocateBuffer<T>(uint count) where T : unmanaged
    {
        return AllocateBuffer(Engine.Utils.ByteSizeOf<T>() * count);
    }
}