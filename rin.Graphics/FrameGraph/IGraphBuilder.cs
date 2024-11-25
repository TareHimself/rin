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

    protected IResourceHandle MakeResourceHandle(IResourceDescriptor descriptor, string name);
    
    public IResourceHandle RequestImage(IPass pass, uint width,uint height,ImageFormat format,VkImageLayout initialLayout,string? name = null);
    public IResourceHandle RequestMemory(IPass pass, ulong size,string? name = null);
    public IResourceHandle Read(IPass pass,string name);
    public IResourceHandle Write(IPass pass,string name);
    public IResourceHandle Read(IPass pass,IResourceHandle handle);
    public IResourceHandle Write(IPass pass,IResourceHandle handle);
    public ICompiledGraph? Compile();

    public void Reset();
}