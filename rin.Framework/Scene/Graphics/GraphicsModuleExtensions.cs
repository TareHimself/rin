using rin.Framework.Core.Extensions;
using rin.Framework.Graphics;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Framework.Scene.Graphics;

public static class GraphicsModuleExtensions
{
    public static async Task<DeviceGeometry> NewGeometry<T>(this SGraphicsModule subsystem, T[] vertices, uint[] indices)
        where T : unmanaged
    {
        var verticesByteSize = vertices.ByteSize();
        var indicesByteSize = indices.ByteSize();
        var vertexBuffer = subsystem.GetAllocator().NewBuffer(verticesByteSize,
            VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_BUFFER_BIT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT, debugName: "Vertex Buffer");

        var indexBuffer = subsystem.GetAllocator().NewBuffer(indicesByteSize,
            VkBufferUsageFlags.VK_BUFFER_USAGE_INDEX_BUFFER_BIT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT, debugName: "Vertex Index Buffer");

        using var stagingBuffer = subsystem.NewTransferBuffer(verticesByteSize + indicesByteSize);

        stagingBuffer.Write(vertices);
        stagingBuffer.Write(indices, verticesByteSize);

        await subsystem.TransferSubmit(cmd =>
        {
            var vertexCopy = new VkBufferCopy
            {
                size = (ulong)verticesByteSize,
                dstOffset = 0,
                srcOffset = 0
            };


            var indicesCopy = new VkBufferCopy
            {
                size = (ulong)indicesByteSize,
                srcOffset = (ulong)verticesByteSize,
                dstOffset = 0
            };

            unsafe
            {
                vkCmdCopyBuffer(cmd, stagingBuffer.NativeBuffer, vertexBuffer.NativeBuffer, 1, &vertexCopy);
                vkCmdCopyBuffer(cmd, stagingBuffer.NativeBuffer, indexBuffer.NativeBuffer, 1, &indicesCopy);
            }
        });
        

        return new DeviceGeometry
        {
            VertexBuffer = vertexBuffer,
            IndexBuffer = indexBuffer
        };
    }
}