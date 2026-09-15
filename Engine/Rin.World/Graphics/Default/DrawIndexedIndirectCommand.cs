namespace Rin.World.Graphics.Default;

/// <summary>
///     Binary-compatible with Vulkan's VkDrawIndexedIndirectCommand (indexCount, instanceCount, firstIndex,
///     vertexOffset, firstInstance as u32/u32/u32/i32/u32) so buffers written with this struct can be consumed
///     directly by vkCmdDrawIndexedIndirectCount.
/// </summary>
public struct DrawIndexedIndirectCommand
{
    public required uint IndexCount;
    public required uint InstanceCount;
    public required uint FirstIndex;
    public required int VertexOffset;
    public required uint FirstInstance;
}
