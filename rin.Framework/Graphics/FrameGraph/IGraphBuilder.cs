using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public interface IGraphBuilder
{
    /// <summary>
    /// Adds a pass to this graph with a random name if one was not supplied
    /// </summary>
    /// <param name="pass"></param>
    /// <returns></returns>
    public uint AddPass(IPass pass);
    public ICompiledGraph? Compile(IImagePool imagePool,Frame frame);
    public void Reset();
}