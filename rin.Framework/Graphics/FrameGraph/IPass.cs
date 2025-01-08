using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public interface IPass : IDisposable
{
    
    public void Configure(IGraphConfig config);

    public void Execute(ICompiledGraph graph,Frame frame,VkCommandBuffer cmd);
    
    public uint Id { get; set; }
    
    
    /// <summary>
    /// A terminal pass is one that represents a valid endpoint of the frame graph, used for pruning, if a <see cref="IGraphBuilder"/> has no terminal passes, nothing will be drawn
    /// </summary>
    public bool IsTerminal { get; }
}