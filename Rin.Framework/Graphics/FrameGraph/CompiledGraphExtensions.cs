namespace Rin.Framework.Graphics.FrameGraph;

public static class CompiledGraphExtensions
{
    public static IGraphImage? GetImageOrNull(this ICompiledGraph graph, uint imageId)
    {
        return imageId <= 0 ? null : graph.GetImage(imageId);
    }

    public static IGraphImage GetImageOrException(this ICompiledGraph graph, uint imageId)
    {
        return imageId <= 0 ? throw new NullReferenceException() : graph.GetImage(imageId);
    }

    public static DeviceBufferView GetBufferOrNull(this ICompiledGraph graph, uint bufferId)
    {
        return bufferId <= 0 ? new DeviceBufferView() : graph.GetBuffer(bufferId);
    }

    public static DeviceBufferView GetBufferOrException(this ICompiledGraph graph, uint bufferId)
    {
        return bufferId <= 0 ? throw new NullReferenceException() : graph.GetBuffer(bufferId);
    }
}