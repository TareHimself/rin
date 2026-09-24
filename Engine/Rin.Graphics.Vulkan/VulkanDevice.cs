using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan;

public sealed class VulkanDevice : IDevice
{
    public required bool SupportsIndirectRendering { get; init; }
}
