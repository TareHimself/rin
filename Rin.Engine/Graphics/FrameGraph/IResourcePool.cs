namespace Rin.Engine.Graphics.FrameGraph;

public interface IResourcePool : IDisposable
{
    public IGraphImage CreateImage(ImageResourceDescriptor descriptor, Frame frame);

    public IDeviceBuffer CreateBuffer(BufferResourceDescriptor descriptor, Frame frame);

    public void OnFrameStart(ulong newFrame);
}