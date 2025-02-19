namespace rin.Framework.Graphics.FrameGraph;

public class ActionPass(
    Action<IPass, IGraphConfig> configure,
    Action<IPass, ICompiledGraph, Frame, IRenderContext> run,
    bool terminal = false,
    string? name = null) : IPass
{
    public string Name { get; } = name ?? $"unknown-pass-{Guid.NewGuid().ToString()}";

    public void BeforeAdd(IGraphBuilder builder)
    {
    }

    public void Configure(IGraphConfig config)
    {
        configure(this, config);
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        run(this, graph, frame, context);
    }

    public uint Id { get; set; }

    public bool IsTerminal => terminal;

    public void Dispose()
    {
    }
}