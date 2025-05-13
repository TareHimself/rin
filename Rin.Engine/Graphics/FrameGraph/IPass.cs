using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

public interface IPass
{
    /// <summary>
    ///     The id of this pass, zero if invalid and greater than zero if valid
    /// </summary>
    public uint Id { get; set; }


    /// <summary>
    ///     A terminal pass is one that represents a valid endpoint of the frame graph, used for pruning, if a
    ///     <see cref="IGraphBuilder" /> has no terminal passes, nothing will be drawn
    /// </summary>
    public bool IsTerminal { get; }

    public bool HandlesPreAdd { get; }
    public bool HandlesPostAdd { get; }

    public void PreAdd(IGraphBuilder builder);

    /// <summary>
    ///     <see cref="IGraphBuilder" /> and has been assigned an ID
    /// </summary>
    /// <param name="builder"></param>
    public void PostAdd(IGraphBuilder builder);

    /// <summary>
    ///     Called when all passes have been added and the graph is being compiled. Perform the least amount of work required
    ///     to figure out pass requirements
    /// </summary>
    /// <param name="config"></param>
    public void Configure(IGraphConfig config);

    /// <summary>
    ///     Called during graph execution to run this pass
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="cmd"></param>
    /// <param name="frame"></param>
    /// <param name="context"></param>
    public void Execute(ICompiledGraph graph, in VkCommandBuffer cmd, Frame frame, IRenderContext context);
}