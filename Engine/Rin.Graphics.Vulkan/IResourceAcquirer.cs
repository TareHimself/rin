using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan;

internal interface IResourceAcquirer
{
    bool TryAcquireResource(in ResourceHandle handle);
}
