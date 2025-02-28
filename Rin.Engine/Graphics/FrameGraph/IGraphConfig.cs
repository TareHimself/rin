namespace Rin.Engine.Graphics.FrameGraph;

public interface IGraphConfig
{
    public uint CreateImage(uint width, uint height, ImageFormat format,
        ImageLayout initialLayout = ImageLayout.Undefined);

    public uint AllocateBuffer(ulong size);
    public uint Read(uint resourceId);
    public uint Write(uint resourceId);

    public uint DependOn(uint passId);

    public uint AllocateBuffer<T>() where T : unmanaged
    {
        return AllocateBuffer(Core.Utils.ByteSizeOf<T>());
    }

    public uint AllocateBuffer<T>(int count) where T : unmanaged
    {
        return AllocateBuffer(Core.Utils.ByteSizeOf<T>(count));
    }
}