namespace Rin.Framework.Graphics.Graph;

public static class Extensions
{
    public static uint AddPass(this IGraphBuilder builder, Action<IPass, IGraphConfig> configure,
        Action<IPass, ICompiledGraph, IExecutionContext> run, bool terminal = false, string? name = null)
    {
        return builder.AddPass(new ActionPass(configure, run, terminal, name));
    }
}