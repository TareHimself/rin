using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Images;

/// <summary>
///     Registry entry for a buffer. Buffers are never bindless-indexed (they're read on the GPU by
///     device address, not by a descriptor slot), but wrapping them the same way as
///     <see cref="BindlessTexture" />/<see cref="BindlessTextureArray" />/<see cref="BindlessCubemap" />
///     gives them the same state and per-slot <see cref="Generation" />
///     bookkeeping, instead of the bare nullable list they used to be stored in.
/// </summary>
public class BindlessBuffer : BindlessResource
{
    public IVulkanDeviceBuffer? Source { get; set; }
    public bool HostVisible { get; set; }
}
