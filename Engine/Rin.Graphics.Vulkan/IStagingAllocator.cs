using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     Hands out the host-writable transfer buffers a graph stages its uploads in.
/// </summary>
internal interface IStagingAllocator
{
    ResourceHandle Allocate(ulong size);
    void Free(in ResourceHandle handle);
}
