using System.Collections.Frozen;
using System.Diagnostics;
using System.Numerics;
using Rin.Framework.Graphics.Descriptors;
using Rin.Framework.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Framework.Graphics;

public class VulkanExecutionContext(
    in VkCommandBuffer commandBuffer,
    DescriptorAllocator allocator,
    FrozenDictionary<uint, DescriptorSet>? globalSets = null) : IExecutionContext
{
    private static readonly VkStencilFaceFlags _stencilFaceFlags = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;

    private readonly FrozenDictionary<uint, DescriptorSet> _globalDescriptorSets =
        globalSets ?? FrozenDictionary<uint, DescriptorSet>.Empty;

    public readonly VkCommandBuffer CommandBuffer = commandBuffer;
    public DescriptorAllocator DescriptorAllocator { get; } = allocator;

    //private bool _primaryAvailable = true;
    public string Id { get; } = Guid.NewGuid().ToString();

    public IExecutionContext BindIndexBuffer(in DeviceBufferView view)
    {
        Debug.Assert(view.IsValid, "Index buffer is not valid");
        vkCmdBindIndexBuffer(CommandBuffer, view.Buffer.NativeBuffer, 0, VkIndexType.VK_INDEX_TYPE_UINT32);
        return this;
    }

    public IExecutionContext Barrier(IImage2D image, ImageLayout from, ImageLayout to)
    {
        CommandBuffer.ImageBarrier(image, from, to);
        return this;
    }

    public IExecutionContext Barrier(in DeviceBufferView view, BufferUsage from, BufferUsage to,
        ResourceOperation fromOperation, ResourceOperation toOperation)
    {
        CommandBuffer.BufferBarrier(view, from, to, fromOperation, toOperation);
        return this;
    }

    public IExecutionContext CopyToBuffer(in DeviceBufferView src, in DeviceBufferView dest)
    {
        Debug.Assert(src.IsValid, "src buffer is not valid");
        Debug.Assert(dest.IsValid, "dest buffer is not valid");
        unsafe
        {
            var copy = new VkBufferCopy
            {
                size = src.Size,
                dstOffset = dest.Offset,
                srcOffset = src.Offset
            };
            vkCmdCopyBuffer(CommandBuffer, src.Buffer.NativeBuffer, dest.Buffer.NativeBuffer, 1, &copy);
        }

        return this;
    }

    public IExecutionContext CopyToImage(in DeviceBufferView src, IImage2D dest)
    {
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
            imageExtent = dest.Extent.ToVk()
        };

        CommandBuffer.CopyBufferToImage(src, dest, [copyRegion]);
        return this;
    }

    public IExecutionContext CopyToImage(IImage2D src, in Offset2D srcOffset, in Extent2D srcSize, IImage2D dest,
        in Offset2D destOffset, in Extent2D destSize, ImageFilter filter = ImageFilter.Linear)
    {
        var vkSrc = (IVulkanImage2D)src;
        var vkDst = (IVulkanImage2D)dest;
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
            z = (int)src.Extent.Dimensions
        };
        blitRegion.dstOffsets[1] = new VkOffset3D
        {
            x = (int)(destOffset.X + destSize.Width),
            y = (int)(destOffset.Y + destSize.Height),
            z = (int)dest.Extent.Dimensions
        };
        unsafe
        {
            var blitInfo = new VkBlitImageInfo2
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_BLIT_IMAGE_INFO_2,
                srcImage = vkSrc.NativeImage,
                dstImage = vkDst.NativeImage,
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
    
    public IExecutionContext CopyToImage(IImage2D src, IImage2D dest, ImageFilter filter = ImageFilter.Linear)
    {
        CommandBuffer.CopyImageToImage(src, dest, filter);
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

    public IExecutionContext BeginRendering(in Extent2D extent, IEnumerable<IImage2D> attachments,
        IImage2D? depthAttachment = null,
        IImage2D? stencilAttachment = null, Vector4? clearColor = null)
    {
        Debug.Assert(depthAttachment == null || depthAttachment.Format == ImageFormat.Depth,
            $"Depth attachment format must be {ImageFormat.Depth}");
        Debug.Assert(stencilAttachment == null || stencilAttachment.Format == ImageFormat.Stencil,
            $"Depth attachment format must be {ImageFormat.Stencil}");

        CommandBuffer
            .BeginRendering(extent, attachments.Select(c => c.MakeColorAttachmentInfo(clearColor)).ToArray(),
                depthAttachment?.MakeDepthAttachmentInfo(), stencilAttachment?.MakeStencilAttachmentInfo())
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
            vkCmdSetStencilReference(CommandBuffer, _stencilFaceFlags, 255);
            vkCmdSetStencilWriteMask(CommandBuffer, _stencilFaceFlags, 0x01);
            vkCmdSetStencilCompareMask(CommandBuffer, _stencilFaceFlags, 0x01);
            vkCmdSetStencilOp(CommandBuffer, _stencilFaceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
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
        vkCmdSetStencilOp(CommandBuffer, _stencilFaceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkStencilOp.VK_STENCIL_OP_REPLACE, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkCompareOp.VK_COMPARE_OP_ALWAYS);
        return this;
    }

    public IExecutionContext StencilCompareOnly()
    {
        vkCmdSetStencilOp(CommandBuffer, _stencilFaceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkStencilOp.VK_STENCIL_OP_KEEP, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkCompareOp.VK_COMPARE_OP_NOT_EQUAL);
        return this;
    }

    public IExecutionContext SetStencilWriteMask(uint mask)
    {
        vkCmdSetStencilWriteMask(CommandBuffer, _stencilFaceFlags, mask);
        return this;
    }

    public IExecutionContext SetStencilWriteValue(uint value)
    {
        vkCmdSetStencilReference(CommandBuffer, _stencilFaceFlags, value);
        return this;
    }

    public IExecutionContext SetStencilCompareMask(uint mask)
    {
        vkCmdSetStencilCompareMask(CommandBuffer, _stencilFaceFlags, mask);
        return this;
    }

    public IExecutionContext ClearColorImages(in Vector4 clearColor, ImageLayout layout, params IImage2D[] images)
    {
        CommandBuffer.ClearColorImages(clearColor, layout, images);
        return this;
    }

    public IExecutionContext ClearStencilImages(uint clearValue, ImageLayout layout, params IImage2D[] images)
    {
        CommandBuffer.ClearStencilImages(clearValue, layout, images);
        return this;
    }

    public IExecutionContext ClearDepthImages(float clearValue, ImageLayout layout, params IImage2D[] images)
    {
        CommandBuffer.ClearDepthImages(clearValue, layout, images);
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