namespace Rin.Framework.Graphics.FrameGraph;

public interface IGraphBuilder
{
    public uint AddPass(IPass pass);

    public ICompiledGraph? Compile(IResourcePool resourcePool, Frame frame);

    public uint AddExternalImage(IImage2D image, Action? onDispose = null);

    public uint AddSwapchainImage(IImage2D image, Action? onDispose = null);

    public void Reset();
}