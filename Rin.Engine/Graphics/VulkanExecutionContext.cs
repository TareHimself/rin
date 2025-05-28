using System.Diagnostics;
using System.Numerics;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Graphics;

public class VulkanExecutionContext(in VkCommandBuffer commandBuffer, DescriptorAllocator allocator) : IExecutionContext
{
    public readonly VkCommandBuffer CommandBuffer = commandBuffer;

    private static VkStencilFaceFlags _stencilFaceFlags = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;
    //private bool _primaryAvailable = true;
    public DescriptorAllocator DescriptorAllocator { get; } = allocator;
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

    public IExecutionContext BindIndexBuffer(IDeviceBufferView buffer)
    {
        vkCmdBindIndexBuffer(CommandBuffer, buffer.NativeBuffer, 0, VkIndexType.VK_INDEX_TYPE_UINT32);
        return this;
    }

    public IExecutionContext Draw(uint vertices, uint instances = 1, uint firstVertex = 0, uint firstInstance = 0)
    {
        vkCmdDraw(CommandBuffer,vertices,instances,firstVertex,firstInstance);
        return this;
    }

    public IExecutionContext DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, uint firstVertex = 0,
        uint firstInstance = 0)
    {
        vkCmdDrawIndexed(CommandBuffer,indexCount,instanceCount,firstIndex,(int)firstVertex,firstInstance);
        return this;
    }

    public IExecutionContext Dispatch(uint x, uint y = 1, uint z = 1)
    {
        vkCmdDispatch(CommandBuffer, x, y, z);
        return this;
    }

    public IExecutionContext Barrier(IDeviceImage image, ImageLayout from, ImageLayout to)
    {
        CommandBuffer.ImageBarrier(image,from,to);
        return this;
    }

    public IExecutionContext Barrier(IDeviceBufferView view, BufferUsage from, BufferUsage to,
        ResourceOperation fromOperation, ResourceOperation toOperation)
    {
        CommandBuffer.BufferBarrier(view,from,to,fromOperation, toOperation);
        return this;
    }

    public IExecutionContext CopyToImage(IDeviceImage dest, IDeviceBufferView src)
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

    public IExecutionContext CopyToImage(IDeviceImage dest, IDeviceImage src, ImageFilter filter = ImageFilter.Linear)
    {
        CommandBuffer.CopyImageToImage(src, dest,filter);
        return this;
    }

    public IExecutionContext DrawIndexedIndirect(IDeviceBufferView commands, uint drawCount, uint stride,uint commandsOffset = 0)
    {
        vkCmdDrawIndexedIndirect(CommandBuffer,commands.NativeBuffer,commands.Offset,drawCount, stride);
        return this;
    }

    public IExecutionContext DrawIndexedIndirectCount(IDeviceBufferView commands, IDeviceBufferView drawCount,
        uint maxDrawCount,
        uint stride,uint commandsOffset = 0, uint drawCountOffset = 0)
    {
        vkCmdDrawIndexedIndirectCount(CommandBuffer,commands.NativeBuffer,commands.Offset,drawCount.NativeBuffer,drawCount.Offset,maxDrawCount,(uint)Utils.ByteSizeOf<VkDrawIndexedIndirectCommand>());
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

    public IExecutionContext BeginRendering(in Extent2D extent, IEnumerable<IDeviceImage> attachments, IDeviceImage? depthAttachment = null,
        IDeviceImage? stencilAttachment = null)
    {
        Debug.Assert(depthAttachment == null || depthAttachment.Format == ImageFormat.Depth,$"Depth attachment format must be {ImageFormat.Depth}");
        Debug.Assert(stencilAttachment == null || stencilAttachment.Format == ImageFormat.Stencil,$"Depth attachment format must be {ImageFormat.Stencil}");
        
        CommandBuffer
            .BeginRendering(extent, attachments.Select(c => c.MakeColorAttachmentInfo()),
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
                vkCmdSetStencilOp(CommandBuffer, _stencilFaceFlags, VkStencilOp.VK_STENCIL_OP_KEEP, VkStencilOp.VK_STENCIL_OP_KEEP,
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
        vkCmdSetDepthTestEnable(CommandBuffer,1);
        return this;
    }

    public IExecutionContext DisableDepthTest()
    {
        vkCmdSetDepthTestEnable(CommandBuffer,0);
        return this;
    }


    public IExecutionContext EnableDepthWrite()
    {
        vkCmdSetDepthWriteEnable(CommandBuffer,1);
        return this;
    }

    public IExecutionContext DisableDepthWrite()
    {
        vkCmdSetDepthWriteEnable(CommandBuffer,0);
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
        vkCmdSetStencilOp(CommandBuffer,_stencilFaceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkStencilOp.VK_STENCIL_OP_KEEP, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkCompareOp.VK_COMPARE_OP_NOT_EQUAL);
        return this;
    }

    public IExecutionContext SetStencilWriteMask(uint mask)
    {
        vkCmdSetStencilWriteMask(CommandBuffer,_stencilFaceFlags,mask);
        return this;
    }

    public IExecutionContext SetStencilWriteValue(uint value)
    {
        vkCmdSetStencilReference(CommandBuffer,_stencilFaceFlags,value);
        return this;
    }

    public IExecutionContext SetStencilCompareMask(uint mask)
    {
        vkCmdSetStencilCompareMask(CommandBuffer,_stencilFaceFlags,mask);
        return this;
    }

    public IExecutionContext ClearColorImages(in Vector4 clearColor, ImageLayout layout, params IDeviceImage[] images)
    {
        CommandBuffer.ClearColorImages(clearColor, layout, images);
        return this;
    }

    public IExecutionContext ClearStencilImages(uint clearValue, ImageLayout layout, params IDeviceImage[] images)
    {
        CommandBuffer.ClearStencilImages(clearValue, layout, images);
        return this;
    }

    public IExecutionContext ClearDepthImages(float clearValue, ImageLayout layout, params IDeviceImage[] images)
    {
        CommandBuffer.ClearDepthImages(clearValue, layout, images);
        return this;
    }
}