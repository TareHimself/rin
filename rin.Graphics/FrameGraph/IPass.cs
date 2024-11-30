using TerraFX.Interop.Vulkan;

namespace rin.Graphics.FrameGraph;

public interface IPass : IDisposable
{
    
    public void Configure(IGraphBuilder builder);

    public void Execute(ICompiledGraph graph,Frame frame,VkCommandBuffer cmd);
    
    public string Name { get; }
    
    
    /// <summary>
    /// A terminal pass is one that represents a valid endpoint of the frame graph, used for pruning, if a <see cref="IGraphBuilder"/> has no terminal passes, nothing will be drawn
    /// </summary>
    public bool IsTerminal { get; }
}