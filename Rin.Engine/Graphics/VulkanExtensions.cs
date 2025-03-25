using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Engine.Core;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

using static TerraFX.Interop.Vulkan.Vulkan;

public static class VulkanExtensions
{
    public static unsafe void* GetAddressProc(this VkDevice device, string name)
    {
        return vkGetDeviceProcAddr(device, (sbyte*)&name);
    }

    public static unsafe void* GetAddressProc(this VkInstance instance, string name)
    {
        return vkGetInstanceProcAddr(instance, (sbyte*)&name);
    }

    [PublicAPI]
    public static VkImageLayout ToVk(this ImageLayout layout)
    {
        return layout switch
        {
            ImageLayout.Undefined => VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            ImageLayout.General => VkImageLayout.VK_IMAGE_LAYOUT_GENERAL,
            ImageLayout.ColorAttachment => VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
            ImageLayout.StencilAttachment => VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL,
            ImageLayout.DepthAttachment => VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_ATTACHMENT_OPTIMAL,
            ImageLayout.ShaderReadOnly => VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
            ImageLayout.TransferSrc => VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
            ImageLayout.TransferDst => VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
            ImageLayout.PresentSrc => VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR,
            _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null)
        };
    }

    // public enum ImageUsage
    // {
    //     None   = 0,
    //     TransferSrc  = 1 << 0,
    //     TransferDst = 1 << 1,
    //     Sampled  = 1 << 2,
    //     Storage = 1 << 3,
    //     ColorAttachment = 1 << 4,
    //     DepthAttachment = 1 << 5,
    //     StencilAttachment = 1 << 6,
    // }
    [PublicAPI]
    public static VkImageUsageFlags ToVk(this ImageUsage usage)
    {
        VkImageUsageFlags flags = 0;

        if (usage.HasFlag(ImageUsage.TransferSrc))
        {
            flags |= VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT;
        }

        if (usage.HasFlag(ImageUsage.TransferDst))
        {
            flags |= VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT;
        }

        if (usage.HasFlag(ImageUsage.Sampled))
        {
            flags |= VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT;
        }

        if (usage.HasFlag(ImageUsage.Storage))
        {
            flags |= VkImageUsageFlags.VK_IMAGE_USAGE_STORAGE_BIT;
        }

        if (usage.HasFlag(ImageUsage.ColorAttachment))
        {
            flags |= VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
        }

        if (usage.HasFlag(ImageUsage.DepthAttachment))
        {
            flags |= VkImageUsageFlags.VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT;
        }

        if (usage.HasFlag(ImageUsage.StencilAttachment))
        {
            flags |= VkImageUsageFlags.VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT;
        }

        return flags;
    }

    [PublicAPI]
    public static VkFormat ToVk(this ImageFormat format)
    {
        return format switch
        {
            ImageFormat.RGBA8 => VkFormat.VK_FORMAT_R8G8B8A8_UNORM,
            ImageFormat.RGBA16 => VkFormat.VK_FORMAT_R16G16B16A16_UNORM,
            ImageFormat.RGBA32 => VkFormat.VK_FORMAT_R32G32B32A32_SFLOAT,
            ImageFormat.Depth => VkFormat.VK_FORMAT_D32_SFLOAT,
            ImageFormat.Stencil => VkFormat.VK_FORMAT_D32_SFLOAT_S8_UINT,
            ImageFormat.R8 => VkFormat.VK_FORMAT_R8_UNORM,
            ImageFormat.R16 => VkFormat.VK_FORMAT_R16_UNORM,
            ImageFormat.R32 => VkFormat.VK_FORMAT_R32_SFLOAT,
            ImageFormat.RG8 => VkFormat.VK_FORMAT_R8G8_UNORM,
            ImageFormat.RG16 => VkFormat.VK_FORMAT_R16G16_UNORM,
            ImageFormat.RG32 => VkFormat.VK_FORMAT_R32G32_SFLOAT,
            ImageFormat.RGB8 => VkFormat.VK_FORMAT_R8G8B8_UNORM,
            ImageFormat.RGB16 => VkFormat.VK_FORMAT_R16G16B16_UNORM,
            ImageFormat.RGB32 => VkFormat.VK_FORMAT_R32G32B32_SFLOAT,
            ImageFormat.Surface => SGraphicsModule.Get().GetSurfaceFormat().format,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [PublicAPI]
    public static VkExtent3D ToVk(this Extent3D extent)
    {
        return new VkExtent3D()
        {
            width = extent.Width,
            height = extent.Height,
            depth = extent.Dimensions
        };
    }

    [PublicAPI]
    public static VkExtent2D ToVk(this Extent2D extent)
    {
        return new VkExtent2D()
        {
            width = extent.Width,
            height = extent.Height
        };
    }

    [PublicAPI]
    public static VkFilter ToVk(this ImageFilter filter)
    {
        return filter switch
        {
            ImageFilter.Linear => VkFilter.VK_FILTER_LINEAR,
            ImageFilter.Nearest => VkFilter.VK_FILTER_NEAREST,
            ImageFilter.Cubic => VkFilter.VK_FILTER_CUBIC_IMG,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [PublicAPI]
    public static VkSamplerAddressMode ToVk(this ImageTiling tiling)
    {
        return tiling switch
        {
            ImageTiling.Repeat => VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_REPEAT,
            ImageTiling.ClampEdge => VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE,
            ImageTiling.ClampBorder => VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_BORDER,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [PublicAPI]
    public static ImageFormat FromVk(this VkFormat format)
    {
        return format switch
        {
            VkFormat.VK_FORMAT_R8G8B8A8_UNORM => ImageFormat.RGBA8,
            VkFormat.VK_FORMAT_R16G16B16A16_UNORM => ImageFormat.RGBA16,
            VkFormat.VK_FORMAT_R32G32B32A32_SFLOAT => ImageFormat.RGBA32,
            VkFormat.VK_FORMAT_D32_SFLOAT => ImageFormat.Depth,
            VkFormat.VK_FORMAT_D32_SFLOAT_S8_UINT => ImageFormat.Stencil,
            _ => format == ImageFormat.Surface.ToVk() ? ImageFormat.Surface : throw new ArgumentOutOfRangeException(),
        };
    }


    public static VkCommandBuffer ClearColorImages(this VkCommandBuffer cmd, Vector4 clearColor, ImageLayout layout,
        params IDeviceImage[] images)
    {
        unsafe
        {
            var vkLayout = layout.ToVk();
            var pColor = stackalloc VkClearColorValue[1];
            var pRanges = stackalloc VkImageSubresourceRange[1];
            pColor[0] = SGraphicsModule.MakeClearColorValue(clearColor);
            pRanges[0] = SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);

            foreach (var deviceImage in images)
                vkCmdClearColorImage(cmd, deviceImage.NativeImage, vkLayout, pColor, 1,
                    pRanges);
        }

        return cmd;
    }

    public static VkCommandBuffer ClearStencilImages(this VkCommandBuffer cmd, uint clearValue, ImageLayout layout,
        params IDeviceImage[] images)
    {
        unsafe
        {
            var vkLayout = layout.ToVk();
            var pColor = stackalloc VkClearDepthStencilValue[1];
            var pRanges = stackalloc VkImageSubresourceRange[1];
            pColor[0] = SGraphicsModule.MakeClearDepthStencilValue(stencil: clearValue);
            pRanges[0] = SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT |
                                                                   VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT);

            foreach (var deviceImage in images)
                vkCmdClearDepthStencilImage(cmd, deviceImage.NativeImage, vkLayout,
                    pColor, 1, pRanges);
        }

        return cmd;
    }

    public static VkCommandBuffer ClearDepthImages(this VkCommandBuffer cmd, float clearValue, ImageLayout layout,
        params IDeviceImage[] images)
    {
        unsafe
        {
            var vkLayout = layout.ToVk();
            var pColor = stackalloc VkClearDepthStencilValue[1];
            var pRanges = stackalloc VkImageSubresourceRange[1];
            pColor[0] = SGraphicsModule.MakeClearDepthStencilValue(clearValue);
            pRanges[0] = SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT);

            foreach (var deviceImage in images)
                vkCmdClearDepthStencilImage(cmd, deviceImage.NativeImage, vkLayout,
                    pColor, 1, pRanges);
        }

        return cmd;
    }

    public static VkCommandBuffer BindShaders(this VkCommandBuffer cmd,
        IEnumerable<Pair<VkShaderEXT, VkShaderStageFlags>> shaders)
    {
        List<VkShaderStageFlags> flagsList = [];
        List<VkShaderEXT> shaderObjects = [];

        foreach (var shader in shaders)
        {
            shaderObjects.Add(shader.First);
            flagsList.Add(shader.Second);
        }

        unsafe
        {
            fixed (VkShaderStageFlags* pFlags = flagsList.ToArray())
            {
                fixed (VkShaderEXT* pShaders = shaderObjects.ToArray())
                {
                    Native.Vulkan.vkCmdBindShadersEXT(cmd, (uint)flagsList.Count, pFlags, pShaders);
                }
            }
        }

        return cmd;
    }

    public static VkCommandBuffer BindShader(this VkCommandBuffer cmd, VkShaderEXT shader, VkShaderStageFlags flags)
    {
        return BindShaders(cmd, [new Pair<VkShaderEXT, VkShaderStageFlags>(shader, flags)]);
    }

    public static VkCommandBuffer UnBindShaders(this VkCommandBuffer cmd, IEnumerable<VkShaderStageFlags> flags)
    {
        var flagsArr = flags.ToArray();
        unsafe
        {
            fixed (VkShaderStageFlags* pFlags = flagsArr)
            {
                Native.Vulkan.vkCmdBindShadersEXT(cmd, (uint)flagsArr.Length, pFlags, null);
            }
        }

        return cmd;
    }

    public static VkCommandBuffer UnBindShader(this VkCommandBuffer cmd, VkShaderStageFlags flag)
    {
        return UnBindShaders(cmd, [flag]);
    }


    public static VkCommandBuffer SetViewports(this VkCommandBuffer cmd, IEnumerable<VkViewport> viewports)
    {
        unsafe
        {
            var items = viewports.ToArray();
            fixed (VkViewport* pItems = items)
            {
                vkCmdSetViewportWithCount(cmd, (uint)items.Length, pItems);
            }
        }

        return cmd;
    }

    public static VkCommandBuffer SetScissors(this VkCommandBuffer cmd, IEnumerable<VkRect2D> scissors)
    {
        unsafe
        {
            var items = scissors.ToArray();
            fixed (VkRect2D* pItems = items)
            {
                vkCmdSetScissorWithCount(cmd, (uint)items.Length, pItems);
            }
        }

        return cmd;
    }

    public static VkCommandBuffer SetRenderArea(this VkCommandBuffer cmd, Vector4 rect)
    {
        return cmd.SetViewports([
            new VkViewport
            {
                x = rect.X,
                y = rect.Y,
                width = rect.Z,
                height = rect.W,
                minDepth = 0.0f,
                maxDepth = 1.0f
            }
        ]).SetScissors([
            new VkRect2D
            {
                offset = new VkOffset2D
                {
                    x = (int)rect.X,
                    y = (int)rect.Y
                },
                extent = new VkExtent2D
                {
                    width = (uint)rect.Z,
                    height = (uint)rect.W
                }
            }
        ]);
    }

    public static VkCommandBuffer SetPolygonMode(this VkCommandBuffer cmd, VkPolygonMode polygonMode,
        float lineWidth = 1.0f)
    {
        Native.Vulkan.vkCmdSetPolygonModeEXT(cmd, polygonMode);
        vkCmdSetLineWidth(cmd, lineWidth);
        return cmd;
    }


    public static VkCommandBuffer SetRasterizerDiscard(this VkCommandBuffer cmd, bool isEnabled)
    {
        vkCmdSetRasterizerDiscardEnable(cmd, (uint)(isEnabled ? 1 : 0));
        return cmd;
    }

    public static VkCommandBuffer DisableMultiSampling(this VkCommandBuffer cmd)
    {
        // _multisampling.sampleShadingEnable = 0;
        // _multisampling.rasterizationSamples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT;
        // _multisampling.minSampleShading = 1.0f;
        // _multisampling.pSampleMask = null;
        // _multisampling.alphaToCoverageEnable = 0;
        // _multisampling.alphaToOneEnable = 0;

        Native.Vulkan.vkCmdSetRasterizationSamplesEXT(cmd, VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT);
        Native.Vulkan.vkCmdSetAlphaToCoverageEnableEXT(cmd, 0);
        Native.Vulkan.vkCmdSetAlphaToOneEnableEXT(cmd, 0);
        unsafe
        {
            uint sampleMask = 0x1;
            Native.Vulkan.vkCmdSetSampleMaskEXT(cmd, VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT, &sampleMask);
        }

        return cmd;
    }

    public static VkCommandBuffer EnableRasterizerDiscard(this VkCommandBuffer cmd)
    {
        return SetRasterizerDiscard(cmd, true);
    }

    public static VkCommandBuffer DisableRasterizerDiscard(this VkCommandBuffer cmd)
    {
        return SetRasterizerDiscard(cmd, false);
    }


    public static VkCommandBuffer SetInputTopology(this VkCommandBuffer cmd, VkPrimitiveTopology topology)
    {
        vkCmdSetPrimitiveTopology(cmd, topology);
        vkCmdSetPrimitiveRestartEnable(cmd, 0);
        return cmd;
    }

    public static VkCommandBuffer SetCullMode(this VkCommandBuffer cmd, VkCullModeFlags cullMode, VkFrontFace frontFace)
    {
        vkCmdSetCullMode(cmd, cullMode);
        vkCmdSetFrontFace(cmd, frontFace);
        return cmd;
    }

    public static VkCommandBuffer DepthWrite(this VkCommandBuffer cmd, bool state)
    {
        vkCmdSetDepthWriteEnable(cmd, (uint)(state ? 1 : 0));
        return cmd;
    }

    public static VkCommandBuffer EnableDepthTest(this VkCommandBuffer cmd, bool depthWriteEnable,
        VkCompareOp compareOp)
    {
        vkCmdSetDepthTestEnable(cmd, 1);
        vkCmdSetDepthWriteEnable(cmd, (uint)(depthWriteEnable ? 1 : 0));
        vkCmdSetDepthCompareOp(cmd, compareOp);
        vkCmdSetDepthBiasEnable(cmd, 0);
        vkCmdSetDepthBoundsTestEnable(cmd, 0);
        // vkCmdSetDepthBounds(cmd,0,1);
        // 
        return cmd;
    }

    public static VkCommandBuffer DisableDepthTest(this VkCommandBuffer cmd, bool depthWriteEnable = false)
    {
        vkCmdSetDepthTestEnable(cmd, 0);
        vkCmdSetDepthWriteEnable(cmd, (uint)(depthWriteEnable ? 1 : 0));
        vkCmdSetDepthBiasEnable(cmd, 0);
        vkCmdSetDepthBoundsTestEnable(cmd, 0);
        return cmd;
    }


    public static VkCommandBuffer DisableStencilTest(this VkCommandBuffer cmd, bool depthWriteEnable)
    {
        vkCmdSetStencilTestEnable(cmd, 0);
        return cmd;
    }

    public static VkCommandBuffer DisableCulling(this VkCommandBuffer cmd)
    {
        return SetCullMode(cmd,
            VkCullModeFlags.VK_CULL_MODE_NONE, VkFrontFace.VK_FRONT_FACE_CLOCKWISE);
    }

    public static VkCommandBuffer SetLogicOpExt(this VkCommandBuffer cmd, VkLogicOp logicOp)
    {
        Native.Vulkan.vkCmdSetLogicOpEXT(cmd, logicOp);

        return cmd;
    }

    public static VkCommandBuffer DisableBlending(this VkCommandBuffer cmd, uint start, uint count,
        VkColorComponentFlags writeMask = VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
                                          VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
                                          VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
                                          VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT)
    {
        unsafe
        {
            Native.Vulkan.vkCmdSetLogicOpEnableEXT(cmd, 0);
            cmd.SetLogicOpExt(VkLogicOp.VK_LOGIC_OP_COPY);
            Native.Vulkan.vkCmdSetLogicOpEnableEXT(cmd, 0);

            fixed (uint* pEnables = Enumerable.Range(0, (int)count).Select(c => (uint)0).ToArray())
            {
                Native.Vulkan.vkCmdSetColorBlendEnableEXT(cmd, start, count, pEnables);
            }

            fixed (VkColorComponentFlags* pWriteMasks =
                       Enumerable.Range(0, (int)count).Select(c => writeMask).ToArray())
            {
                Native.Vulkan.vkCmdSetColorWriteMaskEXT(cmd, start, count, pWriteMasks);
            }

            return cmd;
        }
    }

    public static VkCommandBuffer SetBlendConstants(this VkCommandBuffer cmd, IEnumerable<float> constants)
    {
        var constantsArray = constants.ToArray();
        unsafe
        {
            fixed (float* pConstants = constantsArray)
            {
                vkCmdSetBlendConstants(cmd, pConstants);
                return cmd;
            }
        }
    }

    public static VkCommandBuffer EnableBlending(this VkCommandBuffer cmd, uint start, uint count,
        VkColorBlendEquationEXT equation, VkColorComponentFlags writeMask)
    {
        unsafe
        {
            Native.Vulkan.vkCmdSetLogicOpEnableEXT(cmd, 0);

            fixed (uint* pEnables = Enumerable.Range(0, (int)count).Select(c => (uint)1).ToArray())
            {
                Native.Vulkan.vkCmdSetColorBlendEnableEXT(cmd, start, count, pEnables);
            }

            fixed (VkColorBlendEquationEXT* pEquations =
                       Enumerable.Range(0, (int)count).Select(c => equation).ToArray())
            {
                Native.Vulkan.vkCmdSetColorBlendEquationEXT(cmd, start, count, pEquations);
            }

            fixed (VkColorComponentFlags* pWriteMasks =
                       Enumerable.Range(0, (int)count).Select(c => writeMask).ToArray())
            {
                Native.Vulkan.vkCmdSetColorWriteMaskEXT(cmd, start, count, pWriteMasks);
            }

            return cmd;
        }
    }

    public static VkCommandBuffer SetColorBlendEnable(this VkCommandBuffer cmd, uint start, uint count, bool enable)
    {
        unsafe
        {
            Native.Vulkan.vkCmdSetLogicOpEnableEXT(cmd, 0);

            fixed (uint* pEnables = Enumerable.Range(0, (int)count).Select(c => (uint)(enable ? 1 : 0)).ToArray())
            {
                Native.Vulkan.vkCmdSetColorBlendEnableEXT(cmd, start, count, pEnables);
            }

            return cmd;
        }
    }

    public static VkCommandBuffer SetWriteMask(this VkCommandBuffer cmd, uint start, uint count,
        VkColorComponentFlags writeMask)
    {
        unsafe
        {
            fixed (VkColorComponentFlags* pWriteMasks =
                       Enumerable.Range(0, (int)count).Select(c => writeMask).ToArray())
            {
                Native.Vulkan.vkCmdSetColorWriteMaskEXT(cmd, start, count, pWriteMasks);
            }

            return cmd;
        }
    }

    public static VkCommandBuffer EnableBlendingAdditive(this VkCommandBuffer cmd, uint start, uint count)
    {
        return EnableBlending(cmd, start, count, new VkColorBlendEquationEXT
            {
                srcColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_SRC_ALPHA,
                dstColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE,
                colorBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
                srcAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE,
                dstAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ZERO,
                alphaBlendOp = VkBlendOp.VK_BLEND_OP_ADD
            }, VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
               VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
               VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
               VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT);
    }

    public static VkCommandBuffer EnableBlendingAlphaBlend(this VkCommandBuffer cmd, uint start, uint count)
    {
        return EnableBlending(cmd, start, count, new VkColorBlendEquationEXT
            {
                srcColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_SRC_ALPHA,
                dstColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA,
                colorBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
                srcAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE,
                dstAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA,
                alphaBlendOp = VkBlendOp.VK_BLEND_OP_ADD
            }, VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
               VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
               VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
               VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT);
    }


    public static VkCommandBuffer SetPrimitiveRestart(this VkCommandBuffer cmd, bool isEnabled)
    {
        vkCmdSetPrimitiveRestartEnable(cmd, (uint)(isEnabled ? 1 : 0));
        return cmd;
    }

    public static VkCommandBuffer SetVertexInput(this VkCommandBuffer cmd,
        IEnumerable<VkVertexInputBindingDescription2EXT> bindingDescriptions,
        IEnumerable<VkVertexInputAttributeDescription2EXT> attributeDescriptions)
    {
        unsafe
        {
            var bindingDescriptionsArray = bindingDescriptions.ToArray();
            var attributeDescriptionsArray = attributeDescriptions.ToArray();
            fixed (VkVertexInputBindingDescription2EXT* pBindingDescriptions = bindingDescriptionsArray)
            {
                fixed (VkVertexInputAttributeDescription2EXT* pAttributeDescriptions = attributeDescriptionsArray)
                {
                    Native.Vulkan.vkCmdSetVertexInputEXT(cmd, (uint)bindingDescriptionsArray.Length,
                        pBindingDescriptions, (uint)attributeDescriptionsArray.Length, pAttributeDescriptions);
                }
            }
        }

        return cmd;
    }

    public static VkCommandBuffer BeginRendering(this VkCommandBuffer cmd, VkRect2D rect,
        IEnumerable<VkRenderingAttachmentInfo> attachments, VkRenderingAttachmentInfo? depthAttachment = null,
        VkRenderingAttachmentInfo? stencilAttachment = null)
    {
        unsafe
        {
            var attachmentsArray = attachments.ToArray();
            fixed (VkRenderingAttachmentInfo* pAttachments = attachmentsArray)
            {
                var renderingInfo = SGraphicsModule.MakeRenderingInfo(rect);
                renderingInfo.pColorAttachments = pAttachments;
                renderingInfo.colorAttachmentCount = (uint)attachmentsArray.Length;

                if (depthAttachment.HasValue)
                {
                    var val = depthAttachment.Value;
                    renderingInfo.pDepthAttachment = &val;
                }

                if (stencilAttachment.HasValue)
                {
                    var val = stencilAttachment.Value;
                    renderingInfo.pStencilAttachment = &val;
                }

                vkCmdBeginRendering(cmd, &renderingInfo);
            }
        }

        return cmd;
    }

    public static VkCommandBuffer BeginRendering(this VkCommandBuffer cmd, VkExtent2D extent,
        IEnumerable<VkRenderingAttachmentInfo> attachments, VkRenderingAttachmentInfo? depthAttachment = null,
        VkRenderingAttachmentInfo? stencilAttachment = null)
    {
        return BeginRendering(cmd, new VkRect2D
        {
            offset = new VkOffset2D
            {
                x = 0,
                y = 0
            },
            extent = extent
        }, attachments, depthAttachment, stencilAttachment);
    }


    public static VkCommandBuffer EndRendering(this VkCommandBuffer cmd)
    {
        vkCmdEndRendering(cmd);
        return cmd;
    }

    public static VkCommandBuffer BindDescriptorSets(this VkCommandBuffer cmd, VkPipelineBindPoint bindPoint,
        VkPipelineLayout pipelineLayout, IEnumerable<VkDescriptorSet> sets, uint firstSet = 0)
    {
        unsafe
        {
            var allSets = sets.ToArray();
            fixed (VkDescriptorSet* pSets = allSets)
            {
                vkCmdBindDescriptorSets(cmd, bindPoint,
                    pipelineLayout, firstSet, (uint)allSets.Length, pSets, 0, null);
            }
        }

        return cmd;
    }

    public static VkCommandBuffer BindDescriptorSets(this VkCommandBuffer cmd, VkPipelineBindPoint bindPoint,
        VkPipelineLayout pipelineLayout, IEnumerable<DescriptorSet> sets, uint firstSet = 0)
    {
        unsafe
        {
            var allSets = sets.Select(c => (VkDescriptorSet)c).ToArray();
            fixed (VkDescriptorSet* pSets = allSets)
            {
                var numSets = (uint)allSets.Length;
                vkCmdBindDescriptorSets(cmd, bindPoint,
                    pipelineLayout, firstSet, numSets, pSets, 0, null);
            }
        }

        return cmd;
    }

    public static VkCommandBuffer PushConstant<T>(this VkCommandBuffer cmd, VkPipelineLayout pipelineLayout,
        VkShaderStageFlags stageFlags, T data, uint offset = 0) where T : unmanaged
    {
        
        unsafe
        {
            var size = (uint)Core.Utils.ByteSizeOf<T>();
            #if DEBUG
            if (size > 128)
            {
                Console.WriteLine("PushConstant of size {0} is greater than 128 bytes, this may be an issue on some devices",size);
            }
            #endif
            vkCmdPushConstants(cmd, pipelineLayout,
                stageFlags, offset,size, &data);
        }

        return cmd;
    }

    public static VkShaderEXT[] CreateShaders(this VkDevice device, params VkShaderCreateInfoEXT[] createInfos)
    {
        var shaders = createInfos.Select(c => new VkShaderEXT()).ToArray();
        unsafe
        {
            fixed (VkShaderCreateInfoEXT* pCreateInfos = createInfos)
            {
                fixed (VkShaderEXT* pShaders = shaders)
                {
                    var result = Native.Vulkan.vkCreateShadersEXT(device, (uint)createInfos.Length, pCreateInfos, null,
                        pShaders);
                    if (result !=
                        VkResult.VK_SUCCESS)
                        throw new Exception("Failed to compile shader");
                }
            }
        }

        return shaders;
    }

    // public static VkInstance CreateInstance(ReadOnlySpan<byte> appName,in Version appVersion,ReadOnlySpan<byte> engineName,in Version engineVersion)
    // {
    //     unsafe
    //     {
    //         fixed (byte* pApplicationName = appName)
    //         fixed(byte* pEngineName = engineName)
    //         {
    //             var appInfo = new VkApplicationInfo()
    //             {
    //                 sType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO,
    //                 pApplicationName = (sbyte*)pApplicationName,
    //                 pEngineName = (sbyte*)pEngineName,
    //                 applicationVersion = VK_MAKE_VERSION(appVersion.Major,appVersion.Minor,appVersion.Patch),
    //                 engineVersion = VK_MAKE_VERSION(engineVersion.Major,engineVersion.Minor,engineVersion.Patch),
    //                 apiVersion = VK_API_VERSION_1_3,
    //             };
    //
    //             var instance = new VkInstance();
    //
    //             var instanceInfo = new VkInstanceCreateInfo()
    //             {
    //                 sType = VkStructureType.VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO,
    //                 pApplicationInfo = &appInfo,
    //             };
    //             
    //             vkCreateInstance(&instanceInfo,null, &instance);
    //
    //             return instance;
    //         }
    //     }
    // }

    public static VkResult SignalSemaphore(this VkDevice device, VkSemaphore semaphore)
    {
        unsafe
        {
            var sigInfo = new VkSemaphoreSignalInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SEMAPHORE_SIGNAL_INFO,
                semaphore = semaphore
            };
            return vkSignalSemaphore(device, &sigInfo);
        }
    }


    public static void DestroyShader(this VkDevice device, VkShaderEXT shader)
    {
        unsafe
        {
            Native.Vulkan.vkDestroyShaderEXT(device, shader, null);
        }
    }

    public static VkFence CreateFence(this VkDevice device, bool signaled = false)
    {
        var fenceCreateInfo = new VkFenceCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_FENCE_CREATE_INFO,
            flags = signaled ? VkFenceCreateFlags.VK_FENCE_CREATE_SIGNALED_BIT : 0
        };

        unsafe
        {
            var result = new VkFence();
            vkCreateFence(device, &fenceCreateInfo, null, &result);
            return result;
        }
    }

    public static void DestroyFence(this VkDevice self, VkFence fence)
    {
        unsafe
        {
            vkDestroyFence(self, fence, null);
        }
    }

    public static VkResult ResetFences(this VkDevice self, params VkFence[] fences)
    {
        unsafe
        {
            fixed (VkFence* pFences = fences)
            {
                return vkResetFences(self, (uint)fences.Length, pFences);
            }
        }
    }

    public static VkResult WaitForFences(this VkDevice self, ulong timeout, bool waitAll, params VkFence[] fences)
    {
        unsafe
        {
            fixed (VkFence* pFences = fences)
            {
                return vkWaitForFences(self, (uint)fences.Length, pFences, 1, timeout);
            }
        }
    }

    public static VkSemaphore CreateSemaphore(this VkDevice device)
    {
        var semaphoreCreateInfo = new VkSemaphoreCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO
        };

        unsafe
        {
            VkSemaphore rSemaphore;
            vkCreateSemaphore(device, &semaphoreCreateInfo, null, &rSemaphore);
            return rSemaphore;
        }
    }

    public static void DestroySemaphore(this VkDevice self, VkSemaphore semaphore)
    {
        unsafe
        {
            vkDestroySemaphore(self, semaphore, null);
        }
    }

    public static void DestroySampler(this VkDevice self, VkSampler sampler)
    {
        unsafe
        {
            vkDestroySampler(self, sampler, null);
        }
    }

    public static void Destroy(this VkDevice self)
    {
        unsafe
        {
            vkDestroyDevice(self, null);
        }
    }

    public static void Destroy(this VkInstance self)
    {
        unsafe
        {
            vkDestroyInstance(self, null);
        }
    }

    public static VkCommandBuffer ImageBarrier(this VkCommandBuffer cmd, IGraphImage image,
        ImageLayout to, ImageBarrierOptions? options = null)
    {
        var from = image.Layout;
        image.Layout = to;
        return ImageBarrier(cmd, image.NativeImage, from, to,
            options ?? new ImageBarrierOptions(image.Format, from, to));
    }

    /// <summary>
    ///     The KING of synchronization
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="image"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="options"></param>
    public static VkCommandBuffer ImageBarrier(this VkCommandBuffer cmd, IDeviceImage image, ImageLayout from,
        ImageLayout to, ImageBarrierOptions? options = null)
    {
        return ImageBarrier(cmd, image.NativeImage, from, to,
            options ?? new ImageBarrierOptions(image.Format, from, to));
    }

    /// <summary>
    ///     The KING of synchronization
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="image"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="options"></param>
    public static VkCommandBuffer ImageBarrier(this VkCommandBuffer cmd, VkImage image, ImageLayout from,
        ImageLayout to, ImageBarrierOptions? options = null)
    {
        var opts = options ?? new ImageBarrierOptions();
        var barrier = new VkImageMemoryBarrier2
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER_2,
            srcStageMask = opts.WaitForStages,
            dstStageMask = opts.NextStages,
            srcAccessMask = opts.SrcAccessFlags,
            dstAccessMask = opts.DstAccessFlags,
            oldLayout = from.ToVk(),
            newLayout = to.ToVk(),
            subresourceRange = opts.SubresourceRange,
            image = image
        };

        unsafe
        {
            var depInfo = new VkDependencyInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEPENDENCY_INFO,
                imageMemoryBarrierCount = 1,
                pImageMemoryBarriers = &barrier
            };

            vkCmdPipelineBarrier2(cmd, &depInfo);
        }

        return cmd;
    }

    public static VkCommandBuffer CopyBufferToImage(this VkCommandBuffer cmd, IDeviceBuffer buffer, IDeviceImage image,
        VkBufferImageCopy[] regions, ImageLayout layout = ImageLayout.TransferDst)
    {
        unsafe
        {
            fixed (VkBufferImageCopy* pRegion = regions)
            {
                vkCmdCopyBufferToImage(cmd, buffer.NativeBuffer, image.NativeImage,
                    layout.ToVk(), (uint)regions.Length, pRegion);
            }
        }

        return cmd;
    }

    public static VkCommandBuffer CopyImageToImage(this VkCommandBuffer cmd, IDeviceImage src, IDeviceImage dst,
        ImageFilter filter = ImageFilter.Linear)
    {
        CopyImageToImage(cmd, src.NativeImage, dst.NativeImage, src.Extent, dst.Extent, filter);
        return cmd;
    }

    public static VkCommandBuffer CopyImageToImage(this VkCommandBuffer cmd, IDeviceImage src, VkImage dst,
        Extent3D dstExtent,
        ImageFilter filter = ImageFilter.Linear)
    {
        CopyImageToImage(cmd, src.NativeImage, dst, src.Extent, dstExtent, filter);
        return cmd;
    }

    public static VkCommandBuffer CopyImageToImage(this VkCommandBuffer cmd, IDeviceImage src, IDeviceImage dst,
        Extent3D srcExtent,
        Extent3D dstExtent, ImageFilter filter = ImageFilter.Linear)
    {
        CopyImageToImage(cmd, src.NativeImage, dst.NativeImage, srcExtent, dstExtent, filter);
        return cmd;
    }

    // public static VkCommandBuffer CopyImageToImage(VkCommandBuffer cmd,VkImage src,VkImage dest,VkExtent3D destExtent,VkExtent3D? srcExtent = null)
    // {
    //     cmd.CopyImageToImage(NativeImage,dest,srcExtent.GetValueOrDefault(Extent),destExtent);
    // }

    public static void CopyImageToImage(this VkCommandBuffer cmd, VkImage src, VkImage dst, Extent3D srcExtent,
        Extent3D dstExtent, ImageFilter filter = ImageFilter.Linear)
    {
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

        blitRegion.srcOffsets[1] = new VkOffset3D
        {
            x = (int)srcExtent.Width,
            y = (int)srcExtent.Height,
            z = (int)srcExtent.Dimensions
        };
        blitRegion.dstOffsets[1] = new VkOffset3D
        {
            x = (int)dstExtent.Width,
            y = (int)dstExtent.Height,
            z = (int)dstExtent.Dimensions
        };
        unsafe
        {
            var blitInfo = new VkBlitImageInfo2
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_BLIT_IMAGE_INFO_2,
                srcImage = src,
                dstImage = dst,
                srcImageLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                dstImageLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                filter = filter.ToVk(),
                pRegions = &blitRegion,
                regionCount = 1
            };

            vkCmdBlitImage2(cmd, &blitInfo);
            
        }
    }

    public static VkRenderingAttachmentInfo MakeAttachmentInfo(this IDeviceImage image, ImageLayout newLayout,
        VkClearValue? clearValue = null)
    {
        var attachment = new VkRenderingAttachmentInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_RENDERING_ATTACHMENT_INFO,
            imageView = image.NativeView,
            imageLayout = newLayout.ToVk(),
            loadOp = clearValue == null
                ? VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_LOAD
                : VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
            storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE
        };

        if (clearValue != null) attachment.clearValue = clearValue.Value;

        return attachment;
    }

    public static VkExtent2D ToVkExtent(this Vector2<uint> self)
    {
        return new VkExtent2D()
        {
            height = self.Y,
            width = self.X
        };
    }

    public static VkRenderingAttachmentInfo MakeColorAttachmentInfo(this IDeviceImage image, Vector4? clearValue = null)
    {
        return MakeAttachmentInfo(image, ImageLayout.ColorAttachment, clearValue.HasValue
            ? new VkClearValue
            {
                color = SGraphicsModule.MakeClearColorValue(clearValue.Value)
            }
            : null);
    }

    public static VkRenderingAttachmentInfo MakeDepthAttachmentInfo(this IDeviceImage image, float? clearValue = null)
    {
        return MakeAttachmentInfo(image, ImageLayout.DepthAttachment, clearValue.HasValue
            ? new VkClearValue
            {
                depthStencil = SGraphicsModule.MakeClearDepthStencilValue(clearValue.Value)
            }
            : null);
    }

    public static VkRenderingAttachmentInfo MakeStencilAttachmentInfo(this IDeviceImage image, uint? clearValue = null)
    {
        return MakeAttachmentInfo(image, ImageLayout.StencilAttachment, clearValue.HasValue
            ? new VkClearValue
            {
                depthStencil = SGraphicsModule.MakeClearDepthStencilValue(stencil: clearValue.Value)
            }
            : null);
    }

    public static VkSurfaceFormatKHR[] GetSurfaceFormats(this VkPhysicalDevice physicalDevice, VkSurfaceKHR surface)
    {
        unsafe
        {
            uint numFormats = 0;
            vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, &numFormats, null);
            var result = Enumerable.Range(0, (int)numFormats).Select(c => new VkSurfaceFormatKHR()).ToArray();
            fixed (VkSurfaceFormatKHR* pResults = result)
            {
                vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, &numFormats, pResults);
            }

            return result;
        }
    }

    public static VkPresentModeKHR[] GetSurfacePresentModes(this VkPhysicalDevice physicalDevice, VkSurfaceKHR surface)
    {
        unsafe
        {
            uint numModes = 0;
            vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, &numModes, null);
            var result = Enumerable.Range(0, (int)numModes).Select(c => VkPresentModeKHR.VK_PRESENT_MODE_MAX_ENUM_KHR)
                .ToArray();
            fixed (VkPresentModeKHR* pResults = result)
            {
                vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, &numModes, pResults);
            }

            return result;
        }
    }

    public static void Draw(this VkCommandBuffer self, uint vertices, uint instances = 1, uint firstVertex = 0,
        uint firstInstance = 0)
    {
        vkCmdDraw(self, vertices, instances, firstVertex, firstInstance);
    }


    public static void DestroySurface(this VkInstance self, VkSurfaceKHR surface)
    {
        unsafe
        {
            vkDestroySurfaceKHR(self, surface, null);
        }
    }

    public static VkCommandPool CreateCommandPool(this VkDevice self, uint queueFamilyIndex,
        VkCommandPoolCreateFlags flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT)
    {
        var commandPoolCreateInfo = new VkCommandPoolCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
            flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT,
            queueFamilyIndex = queueFamilyIndex
        };

        VkCommandPool pool = new();
        unsafe
        {
            vkCreateCommandPool(self, &commandPoolCreateInfo, null, &pool);
        }

        return pool;
    }

    public static void DestroyCommandPool(this VkDevice self, VkCommandPool pool)
    {
        unsafe
        {
            vkDestroyCommandPool(self, pool, null);
        }
    }

    public static VkClearColorValue MakeClearColorValue(Vector4 color)
    {
        var clearColor = new VkClearColorValue();
        clearColor.float32[0] = color.X;
        clearColor.float32[1] = color.Y;
        clearColor.float32[2] = color.Z;
        clearColor.float32[3] = color.W;
        return clearColor;
    }

    public static VkClearDepthStencilValue MakeClearDepthStencilValue(float depth = 0.0f, uint stencil = 0)
    {
        var clearColor = new VkClearDepthStencilValue
        {
            depth = depth,
            stencil = stencil
        };
        return clearColor;
    }

    public static VkCommandBuffer[] AllocateCommandBuffers(this VkDevice self, VkCommandPool pool, uint count = 1,
        VkCommandBufferLevel level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY)
    {
        var commandBufferCreateInfo = new VkCommandBufferAllocateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
            commandPool = pool,
            commandBufferCount = count,
            level = level
        };

        var result = new VkCommandBuffer[count];

        unsafe
        {
            fixed (VkCommandBuffer* pResult = result)
            {
                vkAllocateCommandBuffers(self, &commandBufferCreateInfo, pResult);
            }
        }

        return result;
    }

    public static VkRenderingInfo MakeRenderingInfo(VkExtent2D extent)
    {
        return MakeRenderingInfo(new VkRect2D
        {
            offset = new VkOffset2D
            {
                x = 0,
                y = 0
            },
            extent = extent
        });
    }

    public static VkRenderingInfo MakeRenderingInfo(VkRect2D area)
    {
        return new VkRenderingInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_RENDERING_INFO,
            renderArea = area,
            layerCount = 1
        };
    }

    public static VkImageCreateInfo MakeImageCreateInfo(ImageFormat format, Extent3D size, VkImageUsageFlags usage)
    {
        return new VkImageCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
            imageType = VkImageType.VK_IMAGE_TYPE_2D,
            format = format.ToVk(),
            extent = size.ToVk(),
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            tiling = VkImageTiling.VK_IMAGE_TILING_OPTIMAL,
            usage = usage
        };
    }

    public static VkImageViewCreateInfo MakeImageViewCreateInfo(DeviceImage image, VkImageAspectFlags aspect)
    {
        return MakeImageViewCreateInfo(image.Format, image.NativeImage, aspect);
    }

    public static VkImageViewCreateInfo MakeImageViewCreateInfo(ImageFormat format, VkImage image,
        VkImageAspectFlags aspect)
    {
        return new VkImageViewCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
            image = image,
            viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
            format = format.ToVk(),
            subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = aspect,
                baseMipLevel = 0,
                levelCount = VK_REMAINING_MIP_LEVELS,
                baseArrayLayer = 0,
                layerCount = 1
            }
        };
    }


    public static VkImageSubresourceRange MakeImageSubresourceRange(VkImageAspectFlags aspectMask)
    {
        return new VkImageSubresourceRange
        {
            aspectMask = aspectMask,
            baseMipLevel = 0,
            levelCount = VK_REMAINING_MIP_LEVELS,
            baseArrayLayer = 0,
            layerCount = VK_REMAINING_ARRAY_LAYERS
        };
    }

    /// <summary>
    ///     Submits using the specified queue
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="fence"></param>
    /// <param name="commandBuffers"></param>
    /// <param name="signalSemaphores"></param>
    /// <param name="waitSemaphores"></param>
    /// <returns></returns>
    public static void Submit(this VkQueue queue, VkFence fence, VkCommandBufferSubmitInfo[] commandBuffers,
        VkSemaphoreSubmitInfo[]? signalSemaphores = null, VkSemaphoreSubmitInfo[]? waitSemaphores = null)
    {
        unsafe
        {
            var waitArr = waitSemaphores ?? [];
            var signalArr = signalSemaphores ??
                            (waitSemaphores != null ? [] : waitArr);
            fixed (VkSemaphoreSubmitInfo* pWaitSemaphores = waitArr)
            {
                fixed (VkSemaphoreSubmitInfo* pSignalSemaphores = signalArr)
                {
                    fixed (VkCommandBufferSubmitInfo* pCommandBuffers = commandBuffers)
                    {
                        var submit = new VkSubmitInfo2
                        {
                            sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO_2,
                            pCommandBufferInfos = pCommandBuffers,
                            commandBufferInfoCount = (uint)commandBuffers.Length,
                            pSignalSemaphoreInfos = pSignalSemaphores,
                            signalSemaphoreInfoCount = (uint)signalArr.Length,
                            pWaitSemaphoreInfos = pWaitSemaphores,
                            waitSemaphoreInfoCount = (uint)waitArr.Length
                        };

                        vkQueueSubmit2(queue, 1, &submit, fence);
                    }
                }
            }
        }
    }


    public static VkImageView CreateImageView(this VkDevice device, VkImageViewCreateInfo createInfo)
    {
        unsafe
        {
            VkImageView view;
            vkCreateImageView(device, &createInfo, null, &view);
            return view;
        }
    }
}