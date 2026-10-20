using JetBrains.Annotations;
using Rin.Shade;

namespace Rin.Core.Graphics;

/// <summary>
///     Binary-compatible with Vulkan's VkDrawIndirectCommand (vertexCount, instanceCount, firstVertex,
///     firstInstance as u32 each) so buffers written with this struct can be consumed directly by
///     <see cref="IGraphicsBindContext.DrawIndirect" />.
/// </summary>
[NoReorder]
[ShadeExport]
public struct DrawIndirectCommand
{
    public required uint VertexCount;
    public required uint InstanceCount;
    public required uint FirstVertex;
    public required uint FirstInstance;
}
