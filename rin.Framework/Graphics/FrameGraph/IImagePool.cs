namespace rin.Framework.Graphics.FrameGraph;

public interface IImagePool : IDisposable
{
    
    public IDeviceImage CreateImage(ImageResourceDescriptor descriptor,Frame frame);
    
    public void OnFrameStart(ulong newFrame);
}