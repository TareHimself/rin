using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Images;

namespace Rin.Framework.Graphics.Vulkan.Graph;

public static class CompiledGraphExtensions
{
    public static ITexture? GetTextureOrNull(this ICompiledGraph graph, uint imageId)
    {
        return imageId <= 0 ? null : graph.GetTexture(imageId);
    }

    public static ITexture GetTextureOrException(this ICompiledGraph graph, uint imageId)
    {
        return imageId <= 0 ? throw new NullReferenceException() : graph.GetTexture(imageId);
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