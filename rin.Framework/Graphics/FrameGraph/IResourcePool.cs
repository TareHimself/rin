namespace rin.Framework.Graphics.FrameGraph;

public interface IResourcePool : IDisposable
{
    
    public IDeviceImage CreateImage(ImageResourceDescriptor descriptor,Frame frame);
    
    public IDeviceBuffer CreateBuffer(BufferResourceDescriptor descriptor, Frame frame);
    
    public void OnFrameStart(ulong newFrame);
}