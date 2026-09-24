using System.Diagnostics;
using Rin.Core.Graphics;
using Rin.Graphics.Vulkan.Images;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Graphics.Vulkan;

internal static class WriteRecorder
{
    public static ResourceHandle CreateStaging(VulkanGraphicsModule module, ulong size)
    {
        return module.CreateBuffer(size, BufferCreateFlags.TransferSrc | BufferCreateFlags.HostDst);
    }

    public static void RecordTextureCopy(in VkCommandBuffer cmd, IVulkanTexture image, in DeviceBufferView staging,
        PendingTextureWrite write)
    {
        staging.Write(write.Data);

        var region = new VkBufferImageCopy
        {
            bufferOffset = staging.Offset,
            imageSubresource = new VkImageSubresourceLayers
            {
                aspectMask = image.Format.ToAspectFlags(),
                mipLevel = 0,
                baseArrayLayer = 0,
                layerCount = 1
            },
            imageOffset = new VkOffset3D
            {
                x = (int)write.Offset.X,
                y = (int)write.Offset.Y
            },
            imageExtent = new VkExtent3D
            {
                width = write.Extent.Width,
                height = write.Extent.Height,
                depth = 1
            }
        };

        unsafe
        {
            cmd.CopyBufferToImage(staging, image, new Span<VkBufferImageCopy>(&region, 1));
        }
    }

    // Brackets the copies with its own barriers since buffers are also read by device address outside the graph.
    public static void RecordBufferWrites(in VkCommandBuffer cmd, in ResourceHandle target, in DeviceBufferView staging,
        IReadOnlyList<PendingBufferWrite> writes)
    {
        var module = VulkanGraphicsModule.Get();
        var dst = module.ResolveBuffer(target);
        var src = module.ResolveBuffer(staging.Buffer);
        Debug.Assert(dst is not null && src is not null, "Buffer upload target is not resolvable");

        Barrier(cmd, dst!.NativeBuffer,
            VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT, VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT,
            VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT, VkAccessFlags2.VK_ACCESS_2_TRANSFER_WRITE_BIT);

        ulong stagingOffset = 0;
        for (var i = 0; i < writes.Count; i++)
        {
            var write = writes[i];
            for (var j = 0; j < i; j++)
            {
                if (!writes[j].Overlaps(write)) continue;
                Barrier(cmd, dst.NativeBuffer,
                    VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT, VkAccessFlags2.VK_ACCESS_2_TRANSFER_WRITE_BIT,
                    VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT, VkAccessFlags2.VK_ACCESS_2_TRANSFER_WRITE_BIT);
                break;
            }

            staging.Write(write.Data, stagingOffset);
            var region = new VkBufferCopy
            {
                srcOffset = staging.Offset + stagingOffset,
                dstOffset = write.Offset,
                size = (ulong)write.Data.Count
            };
            unsafe
            {
                vkCmdCopyBuffer(cmd, src!.NativeBuffer, dst.NativeBuffer, 1, &region);
            }

            stagingOffset += write.StagingSize;
        }

        Barrier(cmd, dst.NativeBuffer,
            VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT, VkAccessFlags2.VK_ACCESS_2_TRANSFER_WRITE_BIT,
            VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT,
            VkAccessFlags2.VK_ACCESS_2_MEMORY_READ_BIT | VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT);
    }

    public static ulong StagingSize(IReadOnlyList<PendingBufferWrite> writes)
    {
        ulong size = 0;
        foreach (var write in writes) size += write.StagingSize;
        return size;
    }

    private static unsafe void Barrier(in VkCommandBuffer cmd, VkBuffer buffer, VkPipelineStageFlags2 srcStage,
        VkAccessFlags2 srcAccess, VkPipelineStageFlags2 dstStage, VkAccessFlags2 dstAccess)
    {
        var barrier = new VkBufferMemoryBarrier2
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_MEMORY_BARRIER_2,
            srcStageMask = srcStage,
            srcAccessMask = srcAccess,
            dstStageMask = dstStage,
            dstAccessMask = dstAccess,
            buffer = buffer,
            offset = 0,
            size = VK_WHOLE_SIZE
        };
        var dependency = new VkDependencyInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DEPENDENCY_INFO,
            bufferMemoryBarrierCount = 1,
            pBufferMemoryBarriers = &barrier
        };
        vkCmdPipelineBarrier2(cmd, &dependency);
    }
}
