using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Images;
using Rin.Core.Graphics.Shaders;
using Rin.Graphics.Vulkan.Descriptors;
using Rin.Graphics.Vulkan.Images;
using Rin.Graphics.Vulkan.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Graphics.Vulkan;

public class VulkanExecutionContext(
    in VkCommandBuffer commandBuffer,
    DescriptorAllocator allocator,
    FrozenDictionary<uint, DescriptorSet>? globalSets = null) : IExecutionContext
{
    private static readonly VkStencilFaceFlags StencilFaceFlags = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;

    private readonly FrozenDictionary<uint, DescriptorSet> _globalDescriptorSets =
        globalSets ?? FrozenDictionary<uint, DescriptorSet>.Empty;

    public readonly VkCommandBuffer CommandBuffer = commandBuffer;
    public DescriptorAllocator DescriptorAllocator { get; } = allocator;

    //private bool _primaryAvailable = true;
    public string Id { get; } = Guid.NewGuid().ToString();

    public IExecutionContext BindIndexBuffer(in DeviceBufferView view)
    {
        Debug.Assert(view.Buffer is IVulkanDeviceBuffer);
        Debug.Assert(view.IsValid, "Index buffer is not valid");
        vkCmdBindIndexBuffer(CommandBuffer, ((IVulkanDeviceBuffer)view.Buffer).NativeBuffer, 0,
            VkIndexType.VK_INDEX_TYPE_UINT32);
        return this;
    }

    public IExecutionContext Barrier(ITexture image, ImageLayout from, ImageLayout to)
    {
        unsafe
        {
            Debug.Assert(image is IVulkanImage);
            var asVulkanImage = Unsafe.As<IVulkanImage>(image);
            var ops = new ImageBarrierOptions(image.Format,from, to);
            var barrier = new VkImageMemoryBarrier2
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_MEMORY_BARRIER_2,
                srcStageMask = ops.WaitCompleteStages,
                dstStageMask = ops.StartAfterStages,
                srcAccessMask = ops.SrcAccessFlags,
                dstAccessMask = ops.DstAccessFlags,
                oldLayout = from.ToVk(),
                newLayout = to.ToVk(),
                image = asVulkanImage.VulkanImage,
                subresourceRange = ops.SubresourceRange,
            };

            var depInfo = new VkDependencyInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEPENDENCY_INFO,
                imageMemoryBarrierCount = 1,
                pImageMemoryBarriers = &barrier
            };

            vkCmdPipelineBarrier2(CommandBuffer, &depInfo);
        }

        return this;
    }

    

    public IExecutionContext Barrier(ReadOnlySpan<TextureBarrier> barriers)
    {
        unsafe
        {
            const int maxStackBarriers = 6;

            var rented = barriers.Length >= maxStackBarriers ? ArrayPool<VkImageMemoryBarrier2>.Shared.Rent(barriers.Length) : null; 
            try
            {
                var vkBarriers = rented ?? stackalloc VkImageMemoryBarrier2[barriers.Length];
                for (var i = 0; i < barriers.Length; i++)
                {
                    var barrier = barriers[i];
                    var ops = new ImageBarrierOptions(barrier.Texture.Format,barrier.From , barrier.To);
                    Debug.Assert(barrier.Texture is IVulkanImage);
                    var image = (IVulkanImage)barrier.Texture;
                    image.Layout = barrier.To;
                    vkBarriers[i].sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_MEMORY_BARRIER_2;
                    vkBarriers[i].srcStageMask = ops.WaitCompleteStages;
                    vkBarriers[i].dstStageMask = ops.StartAfterStages;
                    vkBarriers[i].srcAccessMask = ops.SrcAccessFlags;
                    vkBarriers[i].dstAccessMask = ops.DstAccessFlags;
                    vkBarriers[i].oldLayout = barrier.From.ToVk();
                    vkBarriers[i].newLayout = barrier.To.ToVk();
                    vkBarriers[i].image = image.VulkanImage;
                    vkBarriers[i].subresourceRange = ops.SubresourceRange;
                }

                fixed (VkImageMemoryBarrier2* pBarriers = vkBarriers)
                {
                    var depInfo = new VkDependencyInfo
                    {
                        sType = VkStructureType.VK_STRUCTURE_TYPE_DEPENDENCY_INFO,
                        imageMemoryBarrierCount = (uint)vkBarriers.Length,
                        pImageMemoryBarriers = pBarriers
                    };

                    vkCmdPipelineBarrier2(CommandBuffer, &depInfo);
                }
            }
            finally
            {
                if (rented != null)
                {
                    ArrayPool<VkImageMemoryBarrier2>.Shared.Return(rented);
                }
            }
        }

        return this;
    }

    public IExecutionContext Barrier(in DeviceBufferView view, BufferUsage from, BufferUsage to,
        ResourceOperation fromOperation, ResourceOperation toOperation)
    {
        unsafe
        {
            var ops = new MemoryBarrierOptions(from, to, fromOperation, toOperation);

            Debug.Assert(view.Buffer is IVulkanDeviceBuffer);
            Debug.Assert(view.IsValid, "Buffer view is not valid");

            var vkBarrier = new VkBufferMemoryBarrier2
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_MEMORY_BARRIER_2,
                srcStageMask = ops.WaitForStages,
                dstStageMask = ops.NextStages,
                srcAccessMask = ops.SrcAccessFlags,
                dstAccessMask = ops.DstAccessFlags,
                buffer = ((IVulkanDeviceBuffer)view.Buffer).NativeBuffer,
                offset = view.Offset,
                size = view.Size
            };

            var depInfo = new VkDependencyInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEPENDENCY_INFO,
                bufferMemoryBarrierCount = 1,
                pBufferMemoryBarriers = &vkBarrier
            };

            vkCmdPipelineBarrier2(CommandBuffer, &depInfo);
        }

        return this;
    }
    
    public IExecutionContext Barrier(ReadOnlySpan<BufferBarrier> barriers)
    {
        
        unsafe
        {
            const int maxStackBarriers = 6;

            var rented = barriers.Length >= maxStackBarriers ? ArrayPool<VkBufferMemoryBarrier2>.Shared.Rent(barriers.Length) : null; 
            try
            {
                var vkBarriers = rented ?? stackalloc VkBufferMemoryBarrier2[barriers.Length];
                for (var i = 0; i < barriers.Length; i++)
                {
                    var barrier = barriers[i];
                    var ops = new MemoryBarrierOptions(barrier.From, barrier.To, barrier.FromOperation,
                        barrier.ToOperation);
                    var view = barrier.View;
                    Debug.Assert(view.Buffer is IVulkanDeviceBuffer);
                    Debug.Assert(view.IsValid, "Buffer view is not valid");
                    vkBarriers[i].sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_MEMORY_BARRIER_2;
                    vkBarriers[i].srcStageMask = ops.WaitForStages;
                    vkBarriers[i].dstStageMask = ops.NextStages;
                    vkBarriers[i].srcAccessMask = ops.SrcAccessFlags;
                    vkBarriers[i].dstAccessMask = ops.DstAccessFlags;
                    vkBarriers[i].buffer = ((IVulkanDeviceBuffer)view.Buffer).NativeBuffer;
                    vkBarriers[i].offset = barrier.View.Offset;
                    vkBarriers[i].size = barrier.View.Size;
                }

                fixed (VkBufferMemoryBarrier2* pBarriers = vkBarriers)
                {
                    var depInfo = new VkDependencyInfo
                    {
                        sType = VkStructureType.VK_STRUCTURE_TYPE_DEPENDENCY_INFO,
                        bufferMemoryBarrierCount = (uint)vkBarriers.Length,
                        pBufferMemoryBarriers = pBarriers
                    };

                    vkCmdPipelineBarrier2(CommandBuffer, &depInfo);
                }
            }
            finally
            {
                if (rented != null)
                {
                    ArrayPool<VkBufferMemoryBarrier2>.Shared.Return(rented);
                }
            }
        }

        return this;
    }

    public IExecutionContext CopyToBuffer(in DeviceBufferView src, in DeviceBufferView dest)
    {
        Debug.Assert(src.IsValid, "src buffer is not valid");
        Debug.Assert(dest.IsValid, "dest buffer is not valid");
        Debug.Assert(src.Buffer is IVulkanDeviceBuffer);
        Debug.Assert(dest.Buffer is IVulkanDeviceBuffer);
        unsafe
        {
            var copy = new VkBufferCopy
            {
                size = src.Size,
                dstOffset = dest.Offset,
                srcOffset = src.Offset
            };
            vkCmdCopyBuffer(CommandBuffer, Unsafe.As<IVulkanDeviceBuffer>(src.Buffer).NativeBuffer,
                Unsafe.As<IVulkanDeviceBuffer>(dest.Buffer).NativeBuffer, 1, &copy);
        }

        return this;
    }

    public IExecutionContext CopyToImage(in DeviceBufferView src, ITexture dest)
    {
        Debug.Assert(src.IsValid);
        Debug.Assert(dest is IVulkanTexture);
        var copyRegion = new VkBufferImageCopy
        {
            bufferOffset = 0,
            bufferRowLength = 0,
            bufferImageHeight = 0,
            imageSubresource = new VkImageSubresourceLayers
            {
                aspectMask = dest.Format.ToAspectFlags(),
                mipLevel = 0,
                baseArrayLayer = 0,
                layerCount = 1
            },
            imageExtent = new VkExtent3D
            {
                width = dest.Extent.Width,
                height = dest.Extent.Height,
                depth = 1
            }
        };

        CommandBuffer.CopyBufferToImage(src, (IVulkanTexture)dest, [copyRegion]);
        return this;
    }

    public IExecutionContext CopyToImage(ITexture src, in Offset2D srcOffset, in Extent2D srcSize, ITexture dest,
        in Offset2D destOffset, in Extent2D destSize, ImageFilter filter = ImageFilter.Linear)
    {
        Debug.Assert(src is IVulkanTexture);
        Debug.Assert(dest is IVulkanTexture);

        var vkSrc = (IVulkanTexture)src;
        var vkDst = (IVulkanTexture)dest;
        var blitRegion = new VkImageBlit2
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_BLIT_2,
            srcSubresource = new VkImageSubresourceLayers
            {
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                baseArrayLayer = 0,
                layerCount = 1,
                mipLevel = 0
            },
            dstSubresource = new VkImageSubresourceLayers
            {
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                baseArrayLayer = 0,
                layerCount = 1,
                mipLevel = 0
            }
        };

        blitRegion.srcOffsets[0] = new VkOffset3D
        {
            x = (int)srcOffset.X,
            y = (int)srcOffset.Y,
            z = 0
        };
        blitRegion.dstOffsets[0] = new VkOffset3D
        {
            x = (int)destOffset.X,
            y = (int)destOffset.Y,
            z = 0
        };

        blitRegion.srcOffsets[1] = new VkOffset3D
        {
            x = (int)(srcOffset.X + srcSize.Width),
            y = (int)(srcOffset.Y + srcSize.Height),
            z = 1
        };
        blitRegion.dstOffsets[1] = new VkOffset3D
        {
            x = (int)(destOffset.X + destSize.Width),
            y = (int)(destOffset.Y + destSize.Height),
            z = 1
        };
        unsafe
        {
            var blitInfo = new VkBlitImageInfo2
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_BLIT_IMAGE_INFO_2,
                srcImage = vkSrc.VulkanImage,
                dstImage = vkDst.VulkanImage,
                srcImageLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                dstImageLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                filter = filter.ToVk(),
                pRegions = &blitRegion,
                regionCount = 1
            };

            vkCmdBlitImage2(CommandBuffer, &blitInfo);
        }

        return this;
    }

    // public IExecutionContext CopyToImage(IDeviceImage src, in Offset2D srcOffset,
    //     IDeviceImage dest,
    //     in Offset2D destOffset,
    //     ImageFilter filter = ImageFilter.Linear) => CopyToImage(src, srcOffset,src.Extent, dest, destOffset,dest.Extent,filter);

    public IExecutionContext CopyToImage(ITexture src, ITexture dest, ImageFilter filter = ImageFilter.Linear)
    {
        Debug.Assert(src is IVulkanTexture);
        Debug.Assert(dest is IVulkanTexture);
        CommandBuffer.CopyImageToImage((IVulkanTexture)src, (IVulkanTexture)dest, filter);
        return this;
    }

    public IExecutionContext EnableBackFaceCulling()
    {
        CommandBuffer.SetCullMode(VkCullModeFlags.VK_CULL_MODE_BACK_BIT, VkFrontFace.VK_FRONT_FACE_CLOCKWISE);
        return this;
    }

    public IExecutionContext EnableFrontFaceCulling()
    {
        CommandBuffer.SetCullMode(VkCullModeFlags.VK_CULL_MODE_FRONT_BIT, VkFrontFace.VK_FRONT_FACE_CLOCKWISE);
        return this;
    }

    public IExecutionContext DisableFaceCulling()
    {
        CommandBuffer.DisableCulling();
        return this;
    }

    public IExecutionContext BeginRendering(in Extent2D extent, ReadOnlySpan<ITexture> attachments,
        ITexture? depthAttachment = null,
        ITexture? stencilAttachment = null, Vector4? clearColor = null)
    {
        Debug.Assert(depthAttachment == null || depthAttachment.Format == ImageFormat.Depth,
            $"Depth attachment format must be {ImageFormat.Depth}");
        Debug.Assert(stencilAttachment == null || stencilAttachment.Format == ImageFormat.Stencil,
            $"Depth attachment format must be {ImageFormat.Stencil}");
        Debug.Assert(depthAttachment is IVulkanImage or null);
        Debug.Assert(stencilAttachment is IVulkanImage or null);
        
        
        const int maxStackBarriers = 6;
        var rentedAttachments = attachments.Length > maxStackBarriers ? ArrayPool<VkRenderingAttachmentInfo>.Shared.Rent(maxStackBarriers) : null;
        try
        {
            var attachmentsSpan = rentedAttachments ?? stackalloc VkRenderingAttachmentInfo[maxStackBarriers];
            for (var i = 0; i < attachments.Length; i++)
            {
                Debug.Assert(attachments[i] is IVulkanTexture);
                attachmentsSpan[i] = ((IVulkanTexture)attachments[i]).MakeColorAttachmentInfo(clearColor);
            }

            CommandBuffer
                .BeginRendering(extent, attachmentsSpan,
                    ((IVulkanTexture?)depthAttachment)?.MakeDepthAttachmentInfo(),
                    ((IVulkanTexture?)stencilAttachment)?.MakeStencilAttachmentInfo())
                .SetViewports([
                    new VkViewport
                    {
                        x = 0,
                        y = 0,
                        width = extent.Width,
                        height = extent.Height,
                        minDepth = 0.0f,
                        maxDepth = 1.0f
                    }
                ])
                .SetScissors([
                    new VkRect2D
                    {
                        offset = new VkOffset2D(),
                        extent = new VkExtent2D
                        {
                            width = extent.Width,
                            height = extent.Height
                        }
                    }
                ]);
        }
        finally
        {
            if (rentedAttachments != null)
            {
                ArrayPool<VkRenderingAttachmentInfo>.Shared.Return(rentedAttachments);
            }
        }

        if (depthAttachment != null)
        {
            vkCmdSetDepthTestEnable(CommandBuffer, 1);
            vkCmdSetDepthWriteEnable(CommandBuffer, 1);
        }
        else
        {
            vkCmdSetDepthTestEnable(CommandBuffer, 0);
        }

        if (stencilAttachment != null)
        {
            vkCmdSetStencilTestEnable(CommandBuffer, 1);
            vkCmdSetStencilReference(CommandBuffer, StencilFaceFlags, 255);
            vkCmdSetStencilWriteMask(CommandBuffer, StencilFaceFlags, 0x01);
            vkCmdSetStencilCompareMask(CommandBuffer, StencilFaceFlags, 0x01);
            vkCmdSetStencilOp(CommandBuffer, StencilFaceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
                VkStencilOp.VK_STENCIL_OP_KEEP,
                VkStencilOp.VK_STENCIL_OP_KEEP, VkCompareOp.VK_COMPARE_OP_NEVER);
        }
        else
        {
            vkCmdSetStencilTestEnable(CommandBuffer, 0);
        }

        return this;
    }

    public IExecutionContext EndRendering()
    {
        CommandBuffer.EndRendering();
        return this;
    }

    public IExecutionContext EnableDepthTest()
    {
        vkCmdSetDepthTestEnable(CommandBuffer, 1);
        return this;
    }

    public IExecutionContext DisableDepthTest()
    {
        vkCmdSetDepthTestEnable(CommandBuffer, 0);
        return this;
    }


    public IExecutionContext EnableDepthWrite()
    {
        vkCmdSetDepthWriteEnable(CommandBuffer, 1);
        return this;
    }

    public IExecutionContext DisableDepthWrite()
    {
        vkCmdSetDepthWriteEnable(CommandBuffer, 0);
        return this;
    }

    public IExecutionContext StencilWriteOnly()
    {
        vkCmdSetStencilOp(CommandBuffer, StencilFaceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkStencilOp.VK_STENCIL_OP_REPLACE, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkCompareOp.VK_COMPARE_OP_ALWAYS);
        return this;
    }

    public IExecutionContext StencilCompareOnly()
    {
        vkCmdSetStencilOp(CommandBuffer, StencilFaceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkStencilOp.VK_STENCIL_OP_KEEP, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkCompareOp.VK_COMPARE_OP_NOT_EQUAL);
        return this;
    }

    public IExecutionContext SetStencilWriteMask(uint mask)
    {
        vkCmdSetStencilWriteMask(CommandBuffer, StencilFaceFlags, mask);
        return this;
    }

    public IExecutionContext SetStencilWriteValue(uint value)
    {
        vkCmdSetStencilReference(CommandBuffer, StencilFaceFlags, value);
        return this;
    }

    public IExecutionContext SetStencilCompareMask(uint mask)
    {
        vkCmdSetStencilCompareMask(CommandBuffer, StencilFaceFlags, mask);
        return this;
    }

    public IExecutionContext ClearColorImages(in Vector4 clearColor, ReadOnlySpan<ITexture> images)
    {
        Debug.Assert(images.ToArray().All(c => c is IVulkanTexture));
        unsafe
        {
            var pColor = stackalloc VkClearColorValue[1];
            var pRanges = stackalloc VkImageSubresourceRange[1];
            pColor[0] = VulkanGraphicsModule.MakeClearColorValue(clearColor);
            pRanges[0] = VulkanGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);
            
            foreach (var image in images)
            {
                var asVulkanImage = Unsafe.As<IVulkanTexture>(image);
                var vkLayout = asVulkanImage.Layout.ToVk();
                vkCmdClearColorImage(CommandBuffer, asVulkanImage.VulkanImage, vkLayout, pColor, 1,
                    pRanges);
            }
        }

        return this;
    }

    public IExecutionContext ClearStencilImages(uint clearValue, ReadOnlySpan<ITexture> images)
    {
        Debug.Assert(images.ToArray().All(c => c.Format == ImageFormat.Stencil));
        Debug.Assert(images.ToArray().All(c => c is IVulkanTexture));
        unsafe
        {
            var pColor = stackalloc VkClearDepthStencilValue[1];
            var pRanges = stackalloc VkImageSubresourceRange[1];
            pColor[0] = VulkanGraphicsModule.MakeClearDepthStencilValue(stencil: clearValue);
            pRanges[0] = VulkanGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT |
                                                                        VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT);
            
            foreach (var image in images)
            {
                var asVulkanImage = Unsafe.As<IVulkanTexture>(image);
                var vkLayout = asVulkanImage.Layout.ToVk();
                vkCmdClearDepthStencilImage(CommandBuffer, asVulkanImage.VulkanImage, vkLayout,
                    pColor, 1, pRanges);
            }
        }

        return this;
    }
    

    public IExecutionContext ClearDepthImages(float clearValue, ReadOnlySpan<ITexture> images)
    {
        Debug.Assert(images.ToArray().All(c => c.Format == ImageFormat.Depth));
        Debug.Assert(images.ToArray().All(c => c is IVulkanTexture));
        unsafe
        {
            var pColor = stackalloc VkClearDepthStencilValue[1];
            var pRanges = stackalloc VkImageSubresourceRange[1];
            pColor[0] = VulkanGraphicsModule.MakeClearDepthStencilValue(clearValue);
            pRanges[0] = VulkanGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT);
            
            foreach (var image in images)
            {
                var asVulkanImage = Unsafe.As<IVulkanTexture>(image);
                var vkLayout = asVulkanImage.Layout.ToVk();
                vkCmdClearDepthStencilImage(CommandBuffer, asVulkanImage.VulkanImage, vkLayout,
                    pColor, 1, pRanges);
            }
        }

        return this;
    }

    public DescriptorSet? FindGlobalDescriptorSet(uint index)
    {
        return _globalDescriptorSets.GetValueOrDefault(index);
    }

    public DescriptorSet AllocateDescriptorSet(IShader shader, uint set)
    {
        Debug.Assert(shader is IVulkanShader);
        var asVk = (IVulkanShader)shader;
        return DescriptorAllocator.Allocate(asVk.GetDescriptorSetLayouts()[set]);
    }

    
    public IExecutionContext BindDescriptorSets(IShader shader, uint offset, params DescriptorSet[] sets)
    {
        Debug.Assert(shader is IVulkanShader);
        var asVk = (IVulkanShader)shader;
        CommandBuffer.BindDescriptorSets(asVk.GetBindPoint(), asVk.GetPipelineLayout(), sets, offset);
        return this;
    }
}