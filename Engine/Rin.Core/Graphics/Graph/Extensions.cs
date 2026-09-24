namespace Rin.Core.Graphics.Graph;

public static class Extensions
{
    public static uint AddPass(this IGraphBuilder builder, Action<IPass, IGraphConfig> configure,
        Action<IPass, ICompiledGraph, IExecutionContext> run, bool terminal = false, string? name = null)
    {
        return builder.AddPass(terminal
            ? new TerminalActionPass(configure, run, name)
            : new ActionPass(configure, run, name));
    }
}