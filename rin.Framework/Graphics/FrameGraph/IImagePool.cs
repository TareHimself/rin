namespace rin.Framework.Graphics.FrameGraph;

public interface IImagePool : IDisposable
{
    
    public IDeviceImage GetOrCreateImage(ImageResourceDescriptor descriptor,string id);
    
    public void OnFrameStart(ulong newFrame);
}