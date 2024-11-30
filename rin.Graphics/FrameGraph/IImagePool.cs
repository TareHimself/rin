namespace rin.Graphics.FrameGraph;

public interface IImagePool : IDisposable
{
    
    public abstract IDeviceImage GetOrCreateImage(ImageResourceDescriptor descriptor,string id);
    
    public void OnFrameStart(ulong newFrame);
}