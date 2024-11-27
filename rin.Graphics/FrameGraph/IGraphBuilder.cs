using TerraFX.Interop.Vulkan;

namespace rin.Graphics.FrameGraph;

public interface IGraphBuilder
{
    /// <summary>
    /// Adds a pass to this graph with a random name if one was not supplied
    /// </summary>
    /// <param name="pass"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public IGraphBuilder AddPass(IPass pass);


    public string RequestImage(IPass pass, uint width,uint height,ImageFormat format,VkImageLayout initialLayout,string? id = null);
    public string RequestMemory(IPass pass, ulong size,string? id = null);
    public string Read(IPass pass,string id);
    public string Write(IPass pass,string id);
    public ICompiledGraph? Compile(IImagePool imagePool);

    public void Reset();
}