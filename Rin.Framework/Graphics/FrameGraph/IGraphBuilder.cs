namespace Rin.Framework.Graphics.FrameGraph;

public interface IGraphBuilder
{
    public uint AddPass(IPass pass);

    public ICompiledGraph? Compile(IResourcePool resourcePool, Frame frame);

    public uint AddExternalImage(IDeviceImage image, Action? onDispose = null);

    public uint AddSwapchainImage(IDeviceImage image, Action? onDispose = null);

    public void Reset();
}