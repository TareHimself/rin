using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public interface IGraphBuilder
{
    /// <summary>
    /// Adds a pass to this graph with a random name if one was not supplied
    /// </summary>
    /// <param name="pass"></param>
    /// <returns></returns>
    public IGraphBuilder AddPass(IPass pass);
    public uint CreateImage(IPass pass, uint width, uint height, ImageFormat format, VkImageLayout initialLayout);
    public uint AllocateBuffer(IPass pass, ulong size);
    public uint Read(IPass pass, uint id);
    public uint Write(IPass pass, uint id);
    public ICompiledGraph? Compile(IImagePool imagePool,Frame frame);

    public void Reset();
}