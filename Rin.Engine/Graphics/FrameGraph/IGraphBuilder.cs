namespace Rin.Engine.Graphics.FrameGraph;

public interface IGraphBuilder
{
    /// <summary>
    ///     Adds a pass to this graph with a random name if one was not supplied
    /// </summary>
    /// <param name="pass"></param>
    /// <returns></returns>
    public uint AddPass(IPass pass);

    public ICompiledGraph? Compile(IResourcePool resourcePool, Frame frame);

    public uint AddExternalImage(IDeviceImage image, Action? onDispose = null);

    public uint AddSwapchainImage(IDeviceImage image, Action? onDispose = null);

    public void Reset();
}