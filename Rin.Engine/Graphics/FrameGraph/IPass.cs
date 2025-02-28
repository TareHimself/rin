namespace Rin.Engine.Graphics.FrameGraph;

public interface IPass : IDisposable
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

    /// <summary>
    ///     Called when this pass is added to a <see cref="IGraphBuilder" /> and has been assigned an ID
    /// </summary>
    /// <param name="builder"></param>
    public void BeforeAdd(IGraphBuilder builder);

    /// <summary>
    ///     Called when all passes have been added and the graph is being compiled
    /// </summary>
    /// <param name="config"></param>
    public void Configure(IGraphConfig config);

    /// <summary>
    ///     Called during graph execution to run this pass
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="frame"></param>
    /// <param name="context"></param>
    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context);
}