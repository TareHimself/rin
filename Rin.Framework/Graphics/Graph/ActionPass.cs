namespace Rin.Framework.Graphics.Graph;

/// <summary>
///     Very basic callback based pass that can be used for debug/simple operations
/// </summary>
/// <param name="configure"></param>
/// <param name="execute"></param>
/// <param name="terminal"></param>
/// <param name="name"></param>
public class ActionPass(
    Action<IPass, IGraphConfig> configure,
    Action<IPass, ICompiledGraph, IExecutionContext> execute,
    bool terminal = false,
    string? name = null) : IPass
{
    public string Name { get; } = name ?? $"unknown-pass-{Guid.NewGuid().ToString()}";

    public void Configure(IGraphConfig config)
    {
        configure(this, config);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        execute(this, graph, ctx);
    }

    public uint Id { get; set; }

    public bool IsTerminal => terminal;
}