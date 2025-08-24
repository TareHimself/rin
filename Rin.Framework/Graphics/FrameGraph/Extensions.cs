namespace Rin.Framework.Graphics.FrameGraph;

public static class Extensions
{
    public static uint AddPass(this IGraphBuilder builder, Action<IPass, IGraphConfig> configure,
        Action<IPass, ICompiledGraph, IExecutionContext> run, bool terminal = false, string? name = null)
    {
        return builder.AddPass(new ActionPass(configure, run, terminal, name));
    }

    public static IDeviceImage AsImage(this IGraphResource resource)
    {
        return resource is IDeviceImage asImage
            ? asImage
            : throw new Exception("Resource is not image");
    }

    public static IDeviceBuffer AsBuffer(this IGraphResource resource)
    {
        return resource is IDeviceBuffer asMemory
            ? asMemory
            : throw new Exception("Resource is not memory");
    }
}