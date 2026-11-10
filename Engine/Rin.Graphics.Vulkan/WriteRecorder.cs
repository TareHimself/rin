using System.Diagnostics;
using Rin.Core.Graphics;
using Rin.Graphics.Vulkan.Images;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Graphics.Vulkan;

internal static class WriteRecorder
{
    public static void RecordTextureCopy(in VkCommandBuffer cmd, IVulkanTexture image, in DeviceBufferView staging,
        PendingTextureWrite write)
    {
        staging.Write(write.Data);
        RecordBufferToImage(cmd, image, staging, write.Offset, write.Extent);
    }

    public static void RecordBufferToImage(in VkCommandBuffer cmd, IVulkanTexture image, in DeviceBufferView buffer,
        in Offset2D offset, in Extent2D extent)
    {
        var region = new VkBufferImageCopy
        {
            bufferOffset = buffer.Offset,
            imageSubresource = new VkImageSubresourceLayers
            {
                aspectMask = image.Format.ToAspectFlags(),
                mipLevel = 0,
                baseArrayLayer = 0,
                layerCount = 1
            },
            imageOffset = new VkOffset3D
            {
                x = (int)offset.X,
                y = (int)offset.Y
            },
            imageExtent = new VkExtent3D
            {
                width = extent.Width,
                height = extent.Height,
                depth = 1
            }
        };

        unsafe
        {
            cmd.CopyBufferToImage(buffer, image, new Span<VkBufferImageCopy>(&region, 1));
        }
    }

    // Brackets each copy with its own barriers and layout changes since the order of copies decides the result.
    public static void RecordCopy(in VkCommandBuffer cmd, CopyAction copy)
    {
        var module = VulkanGraphicsModule.Get();
        switch (copy)
        {
            case BufferCopy bufferCopy:
                RecordBufferCopy(cmd, module, bufferCopy);
                break;
            case TextureCopy textureCopy:
                RecordTextureToTextureCopy(cmd, module, textureCopy);
                break;
            case BufferToTextureCopy toTexture:
                RecordBufferToTextureCopy(cmd, module, toTexture);
                break;
            case TextureToBufferCopy toBuffer:
                RecordTextureToBufferCopy(cmd, module, toBuffer);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(copy), copy, null);
        }
    }

    private static void RecordBufferCopy(in VkCommandBuffer cmd, VulkanGraphicsModule module, BufferCopy copy)
    {
        var src = module.ResolveBuffer(copy.Source);
        var dst = module.ResolveBuffer(copy.Destination);
        Debug.Assert(src is not null && dst is not null, "Buffer copy resource is not resolvable");

        BeginTransfer(cmd, src!.NativeBuffer, VkAccessFlags2.VK_ACCESS_2_TRANSFER_READ_BIT);
        BeginTransfer(cmd, dst!.NativeBuffer, VkAccessFlags2.VK_ACCESS_2_TRANSFER_WRITE_BIT);
        var region = new VkBufferCopy
        {
            srcOffset = copy.SourceOffset,
            dstOffset = copy.DestinationOffset,
            size = copy.Size
        };
        unsafe
        {
            vkCmdCopyBuffer(cmd, src.NativeBuffer, dst.NativeBuffer, 1, &region);
        }

        EndTransfer(cmd, src.NativeBuffer, VkAccessFlags2.VK_ACCESS_2_TRANSFER_READ_BIT);
        EndTransfer(cmd, dst.NativeBuffer, VkAccessFlags2.VK_ACCESS_2_TRANSFER_WRITE_BIT);
    }

    private static void RecordTextureToTextureCopy(in VkCommandBuffer cmd, VulkanGraphicsModule module,
        TextureCopy copy)
    {
        var src = module.GetTexture(copy.Source);
        var dst = module.GetTexture(copy.Destination);
        Debug.Assert(src is not null && dst is not null, "Texture copy resource is not resolvable");

        var restore = RestoreLayout(src!);
        cmd.ImageBarrier(src!, src!.Layout, ImageLayout.TransferSrc);
        cmd.ImageBarrier(dst!, dst!.Layout, ImageLayout.TransferDst);
        cmd.CopyImage(src, dst, copy.SourceOffset, copy.DestinationOffset, copy.Extent);
        cmd.ImageBarrier(src, ImageLayout.TransferSrc, restore);
    }

    private static void RecordBufferToTextureCopy(in VkCommandBuffer cmd, VulkanGraphicsModule module,
        BufferToTextureCopy copy)
    {
        var src = module.ResolveBuffer(copy.Source);
        var dst = module.GetTexture(copy.Destination);
        Debug.Assert(src is not null && dst is not null, "Buffer to texture copy resource is not resolvable");

        BeginTransfer(cmd, src!.NativeBuffer, VkAccessFlags2.VK_ACCESS_2_TRANSFER_READ_BIT);
        cmd.ImageBarrier(dst!, dst!.Layout, ImageLayout.TransferDst);
        RecordBufferToImage(cmd, dst, new DeviceBufferView(copy.Source, copy.SourceOffset, copy.Size),
            copy.DestinationOffset, copy.Extent);
        EndTransfer(cmd, src.NativeBuffer, VkAccessFlags2.VK_ACCESS_2_TRANSFER_READ_BIT);
    }

    private static void RecordTextureToBufferCopy(in VkCommandBuffer cmd, VulkanGraphicsModule module,
        TextureToBufferCopy copy)
    {
        var src = module.GetTexture(copy.Source);
        var dst = module.ResolveBuffer(copy.Destination);
        Debug.Assert(src is not null && dst is not null, "Texture to buffer copy resource is not resolvable");

        var restore = RestoreLayout(src!);
        cmd.ImageBarrier(src!, src!.Layout, ImageLayout.TransferSrc);
        BeginTransfer(cmd, dst!.NativeBuffer, VkAccessFlags2.VK_ACCESS_2_TRANSFER_WRITE_BIT);
        cmd.CopyImageToBuffer(src, copy.SourceOffset, copy.Extent, dst, copy.DestinationOffset);
        EndTransfer(cmd, dst.NativeBuffer, VkAccessFlags2.VK_ACCESS_2_TRANSFER_WRITE_BIT);
        cmd.ImageBarrier(src, ImageLayout.TransferSrc, restore);
    }

    private static ImageLayout RestoreLayout(IVulkanTexture image)
    {
        return image.Layout == ImageLayout.Undefined ? ImageLayout.ShaderReadOnly : image.Layout;
    }

    private static void BeginTransfer(in VkCommandBuffer cmd, VkBuffer buffer, VkAccessFlags2 transferAccess)
    {
        Barrier(cmd, buffer,
            VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT,
            VkAccessFlags2.VK_ACCESS_2_MEMORY_READ_BIT | VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT,
            VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT, transferAccess);
    }

    private static void EndTransfer(in VkCommandBuffer cmd, VkBuffer buffer, VkAccessFlags2 transferAccess)
    {
        Barrier(cmd, buffer,
            VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT, transferAccess,
            VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT,
            VkAccessFlags2.VK_ACCESS_2_MEMORY_READ_BIT | VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT);
    }

    // Brackets the copies with its own barriers since buffers are also read through the bindless set outside the graph.
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
