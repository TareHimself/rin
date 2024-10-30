using System.Collections;
using System.Runtime.InteropServices;
using rin.Core;
using rin.Graphics.Descriptors;
using rin.Graphics.Shaders;
using rin.Core.Math;

namespace rin.Graphics;

using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
using static TerraFX.Interop.Vulkan.VkCommandBufferManualImports;

public static class VulkanExtensions
{
    
    public static unsafe void* GetAddressProc(this VkDevice device, string name) =>
        vkGetDeviceProcAddr(device, (sbyte*)&name);

    public static unsafe void* GetAddressProc(this VkInstance instance, string name) => vkGetInstanceProcAddr(instance, (sbyte*)&name);
    
    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkCmdBindShadersEXT")]
    public static extern unsafe void vkCmdBindShadersEXT(VkCommandBuffer commandBuffer, 
        uint stageCount, 
        VkShaderStageFlags* pStages, 
        VkShaderEXT* pShaders);

    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkCreateShadersEXT")]
    public static extern unsafe VkResult vkCreateShadersEXT(
        VkDevice device,
        uint createInfoCount,
        VkShaderCreateInfoEXT* pCreateInfos,
        VkAllocationCallbacks* pAllocator,
        VkShaderEXT* pShaders);

    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkDestroyShaderEXT")]
    public static extern unsafe void vkDestroyShaderEXT(
        VkDevice device,
        VkShaderEXT shader,
        VkAllocationCallbacks* pAllocator);
    
    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkCmdSetPolygonModeEXT")]
    public static extern unsafe void vkCmdSetPolygonModeEXT(VkCommandBuffer commandBuffer, VkPolygonMode polygonMode);
    
    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkCmdSetLogicOpEXT")]
    public static extern void vkCmdSetLogicOpEXT(VkCommandBuffer commandBuffer, VkLogicOp logicOp);

    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkCmdSetLogicOpEnableEXT")]
    public static extern void vkCmdSetLogicOpEnableEXT(VkCommandBuffer commandBuffer, uint logicOpEnable);
    
    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkCmdSetColorBlendEnableEXT")]
    public static extern unsafe void vkCmdSetColorBlendEnableEXT(
        VkCommandBuffer commandBuffer, 
        uint firstAttachment, 
        uint attachmentCount, 
        uint* pColorBlendEnables);

    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkCmdSetColorBlendEquationEXT")]
    public static extern unsafe void vkCmdSetColorBlendEquationEXT(
        VkCommandBuffer commandBuffer, 
        uint firstAttachment, 
        uint attachmentCount, 
        VkColorBlendEquationEXT* pColorBlendEquations);

    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkCmdSetColorWriteMaskEXT")]
    public static extern unsafe void vkCmdSetColorWriteMaskEXT(
        VkCommandBuffer commandBuffer, 
        uint firstAttachment, 
        uint attachmentCount, 
        VkColorComponentFlags* pColorWriteMasks);
    
    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkCmdSetVertexInputEXT")]
    public static extern unsafe void vkCmdSetVertexInputEXT( VkCommandBuffer commandBuffer, 
        uint vertexBindingDescriptionCount, 
        VkVertexInputBindingDescription2EXT* pVertexBindingDescriptions, 
        uint vertexAttributeDescriptionCount, 
        VkVertexInputAttributeDescription2EXT* pVertexAttributeDescriptions);
    
    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkCmdSetRasterizationSamplesEXT")]
    public static extern unsafe void vkCmdSetRasterizationSamplesEXT( VkCommandBuffer commandBuffer, 
        VkSampleCountFlags rasterizationSamples);

    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkCmdSetAlphaToCoverageEnableEXT")]
    public static extern unsafe void vkCmdSetAlphaToCoverageEnableEXT(VkCommandBuffer commandBuffer, uint alphaToCoverageEnable);

    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkCmdSetAlphaToOneEnableEXT")]
    public static extern unsafe void vkCmdSetAlphaToOneEnableEXT(VkCommandBuffer commandBuffer, uint alphaToOneEnable);

    [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsVkCmdSetSampleMaskEXT")]
    public static extern unsafe void vkCmdSetSampleMaskEXT(
        VkCommandBuffer commandBuffer, 
        VkSampleCountFlags samples, 
        uint* pSampleMask);
    

    // [DllImport(Dlls.AeroxGraphicsNative, EntryPoint = "vkCmdBeginRenderingKHR")]
    // private static extern unsafe void vkCmdBeginRenderingKHR(VkCommandBuffer commandBuffer, 
    // VkRenderingInfo* pRenderingInfo);
    //
    // [DllImport(Dlls.AeroxGraphicsNative, ExactSpelling = true)]
    // private static extern void vkCmdEndRenderingKHR(VkCommandBuffer commandBuffer);
    
    public static VkCommandBuffer BindShaders(this VkCommandBuffer cmd,IEnumerable<Pair<VkShaderEXT,VkShaderStageFlags>> shaders)
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
                    vkCmdBindShadersEXT(cmd,(uint)flagsList.Count, pFlags, pShaders);
                }
            }
        }
        
        return cmd;
    }

    public static VkCommandBuffer BindShader(this VkCommandBuffer cmd, VkShaderEXT shader,VkShaderStageFlags flags) => BindShaders(cmd, [new Pair<VkShaderEXT, VkShaderStageFlags>(shader,flags)]);
    
    public static VkCommandBuffer UnBindShaders(this VkCommandBuffer cmd,IEnumerable<VkShaderStageFlags> flags)
    {
        VkShaderStageFlags[] flagsArr = flags.ToArray();
        unsafe
        {
            fixed (VkShaderStageFlags* pFlags = flagsArr)
            {
                vkCmdBindShadersEXT(cmd,(uint)flagsArr.Length, pFlags, null);
            }
        }
        
        return cmd;
    }

    public static VkCommandBuffer UnBindShader(this VkCommandBuffer cmd, VkShaderStageFlags flag) => UnBindShaders(cmd, [flag]);


    public static VkCommandBuffer SetViewports(this VkCommandBuffer cmd,IEnumerable<VkViewport> viewports)
    {
        unsafe
        {
            var items = viewports.ToArray();
            fixed (VkViewport* pItems = items)
            {
                vkCmdSetViewportWithCount(cmd,(uint)items.Length,pItems);
            }
        }
        return cmd;
    }
    
    public static VkCommandBuffer SetScissors(this VkCommandBuffer cmd,IEnumerable<VkRect2D> scissors)
    {
        unsafe
        {
            var items = scissors.ToArray();
            fixed (VkRect2D* pItems = items)
            {
                vkCmdSetScissorWithCount(cmd,(uint)items.Length,pItems);
            }
        }
        return cmd;
    }

    public static VkCommandBuffer SetRenderArea(this VkCommandBuffer cmd, Vector4<float> rect) => cmd.SetViewports([
        new VkViewport()
        {
            x = rect.X,
            y = rect.Y,
            width = rect.Z,
            height = rect.W,
            minDepth = 0.0f,
            maxDepth = 1.0f
        }
    ]).SetScissors([
        new VkRect2D()
        {
            offset = new VkOffset2D()
            {
                x = (int)rect.X,
                y = (int)rect.Y
            },
            extent = new VkExtent2D()
            {
                width = (uint)rect.Z,
                height = (uint)rect.W
            }
        }
    ]);
    
    public static VkCommandBuffer SetPolygonMode(this VkCommandBuffer cmd,VkPolygonMode polygonMode,float lineWidth = 1.0f)
    {
        vkCmdSetPolygonModeEXT(cmd,polygonMode);
        vkCmdSetLineWidth(cmd,lineWidth);
        return cmd;
    }
    
    public static VkCommandBuffer SetRasterizerDiscard(this VkCommandBuffer cmd,bool isEnabled)
    {
        vkCmdSetRasterizerDiscardEnable(cmd,(uint)(isEnabled ? 1 : 0));
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
        
        vkCmdSetRasterizationSamplesEXT(cmd,VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT);
        vkCmdSetAlphaToCoverageEnableEXT(cmd,0);
        vkCmdSetAlphaToOneEnableEXT(cmd,0);
        unsafe
        {
            uint sampleMask = 0x1;
            vkCmdSetSampleMaskEXT(cmd,VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,&sampleMask);
        }
        return cmd;
    }

    public static VkCommandBuffer EnableRasterizerDiscard(this VkCommandBuffer cmd) =>
        SetRasterizerDiscard(cmd, true);
    
    public static VkCommandBuffer DisableRasterizerDiscard(this VkCommandBuffer cmd) => SetRasterizerDiscard(cmd, false);
    
    
    
    public static VkCommandBuffer SetInputTopology(this VkCommandBuffer cmd,VkPrimitiveTopology topology)
    {
        vkCmdSetPrimitiveTopology(cmd,topology);
        vkCmdSetPrimitiveRestartEnable(cmd,0);
        return cmd;
    }

    public static VkCommandBuffer SetCullMode(this VkCommandBuffer cmd,VkCullModeFlags cullMode,VkFrontFace frontFace)
    {
        vkCmdSetCullMode(cmd,cullMode);
        vkCmdSetFrontFace(cmd,frontFace);
        return cmd;
    }
    
    public static VkCommandBuffer EnableDepthTest(this VkCommandBuffer cmd,bool depthWriteEnable,VkCompareOp compareOp)
    {
        vkCmdSetDepthTestEnable(cmd,1);
        vkCmdSetDepthWriteEnable(cmd,(uint)(depthWriteEnable ? 1 : 0));
        vkCmdSetDepthCompareOp(cmd,compareOp);
        vkCmdSetDepthBiasEnable(cmd,0);
        vkCmdSetDepthBoundsTestEnable(cmd,0);
        // vkCmdSetDepthBounds(cmd,0,1);
        // 
        return cmd;
    }
    
    public static VkCommandBuffer DisableDepthTest(this VkCommandBuffer cmd,bool depthWriteEnable = false)
    {
        vkCmdSetDepthTestEnable(cmd,0);
        vkCmdSetDepthWriteEnable(cmd,(uint)(depthWriteEnable ? 1 : 0));
        vkCmdSetDepthBiasEnable(cmd,0);
        vkCmdSetDepthBoundsTestEnable(cmd,0);
        return cmd;
    }


    public static VkCommandBuffer DisableStencilTest(this VkCommandBuffer cmd, bool depthWriteEnable)
    {
        vkCmdSetStencilTestEnable(cmd, 0);
        return cmd;
    }

    public static VkCommandBuffer DisableCulling(this VkCommandBuffer cmd) => SetCullMode(cmd,
        VkCullModeFlags.VK_CULL_MODE_NONE, VkFrontFace.VK_FRONT_FACE_CLOCKWISE);

    public static VkCommandBuffer SetLogicOpExt(this VkCommandBuffer cmd,VkLogicOp logicOp)
    {
        unsafe
        {
            vkCmdSetLogicOpEXT(cmd,logicOp);
        }
        
        return cmd;
    }
    
    public static VkCommandBuffer DisableBlending(this VkCommandBuffer cmd)
    {
        vkCmdSetLogicOpEnableEXT(cmd,0);
        cmd.SetLogicOpExt(VkLogicOp.VK_LOGIC_OP_COPY);
        return cmd;
    }

    public static VkCommandBuffer SetBlendConstants(this VkCommandBuffer cmd,IEnumerable<float> constants)
    {
        var constantsArray = constants.ToArray();
        unsafe
        {
            fixed (float* pConstants = constantsArray)
            {
                vkCmdSetBlendConstants(cmd,pConstants);
                return cmd;
            }
            
        }
    }

    public static VkCommandBuffer EnableBlending(this VkCommandBuffer cmd,uint start, uint count,VkColorBlendEquationEXT equation,VkColorComponentFlags writeMask)
    {
        unsafe
        {
            vkCmdSetLogicOpEnableEXT(cmd,0);
            
            fixed (uint* pEnables = Enumerable.Range(0, (int)count).Select(c => (uint)1).ToArray())
            {
                vkCmdSetColorBlendEnableEXT(cmd,start,count,pEnables);
            }
            
            fixed (VkColorBlendEquationEXT* pEquations = Enumerable.Range(0, (int)count).Select(c => equation).ToArray())
            {
                vkCmdSetColorBlendEquationEXT(cmd,start,count,pEquations);
            }
            
            fixed (VkColorComponentFlags* pWriteMasks= Enumerable.Range(0, (int)count).Select(c => writeMask).ToArray())
            {
                vkCmdSetColorWriteMaskEXT(cmd,start,count,pWriteMasks);
            }

            return cmd;
        }
    }
    
    public static VkCommandBuffer SetColorBlendEnable(this VkCommandBuffer cmd,uint start, uint count,bool enable)
    {
        unsafe
        {
            vkCmdSetLogicOpEnableEXT(cmd,0);
            
            fixed (uint* pEnables = Enumerable.Range(0, (int)count).Select(c => (uint)(enable ? 1 : 0)).ToArray())
            {
                vkCmdSetColorBlendEnableEXT(cmd,start,count,pEnables);
            }
            return cmd;
        }
    }
    
    public static VkCommandBuffer SetWriteMask(this VkCommandBuffer cmd,uint start, uint count,VkColorComponentFlags writeMask)
    {
        unsafe
        {
            fixed (VkColorComponentFlags* pWriteMasks= Enumerable.Range(0, (int)count).Select(c => writeMask).ToArray())
            {
                vkCmdSetColorWriteMaskEXT(cmd,start,count,pWriteMasks);
            }
            return cmd;
        }
    }

    public static VkCommandBuffer EnableBlendingAdditive(this VkCommandBuffer cmd,uint start, uint count) =>
        EnableBlending(cmd, start, count, new VkColorBlendEquationEXT()
            {
                srcColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE,
                dstColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE_MINUS_DST_ALPHA,
                colorBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
                srcAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE,
                dstAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ZERO,
                alphaBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
            }, VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
               VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
               VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
               VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT);

    public static VkCommandBuffer EnableBlendingAlphaBlend(this VkCommandBuffer cmd,uint start, uint count) => EnableBlending(cmd, start, count, new VkColorBlendEquationEXT()
        {
           srcColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_SRC_ALPHA,
           dstColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA,
           colorBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
           srcAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_SRC_ALPHA,
           dstAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA,
           alphaBlendOp = VkBlendOp.VK_BLEND_OP_SUBTRACT
        }, VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
           VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
           VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
           VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT);
    
    
    public static VkCommandBuffer SetPrimitiveRestart(this VkCommandBuffer cmd,bool isEnabled)
    {
        vkCmdSetPrimitiveRestartEnable(cmd,(uint)(isEnabled ? 1 : 0));
        return cmd;
    }
    
    public static VkCommandBuffer SetVertexInput(this VkCommandBuffer cmd,IEnumerable<VkVertexInputBindingDescription2EXT> bindingDescriptions,IEnumerable<VkVertexInputAttributeDescription2EXT> attributeDescriptions)
    {
        unsafe
        {
            var bindingDescriptionsArray = bindingDescriptions.ToArray();
            var attributeDescriptionsArray = attributeDescriptions.ToArray();
            fixed (VkVertexInputBindingDescription2EXT* pBindingDescriptions = bindingDescriptionsArray)
            {
                fixed (VkVertexInputAttributeDescription2EXT* pAttributeDescriptions = attributeDescriptionsArray)
                {
                    vkCmdSetVertexInputEXT(cmd, (uint)bindingDescriptionsArray.Length,
                        pBindingDescriptions, (uint)attributeDescriptionsArray.Length, pAttributeDescriptions);
                }
            }
        }
        return cmd;
    }
    
    public static VkCommandBuffer BeginRendering(this VkCommandBuffer cmd,VkRect2D rect,IEnumerable<VkRenderingAttachmentInfo> attachments,VkRenderingAttachmentInfo? depthAttachment = null,VkRenderingAttachmentInfo? stencilAttachment = null)
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
                
                vkCmdBeginRendering(cmd,&renderingInfo);
            }
        }
        return cmd;
    }

    public static VkCommandBuffer BeginRendering(this VkCommandBuffer cmd, VkExtent2D extent,
        IEnumerable<VkRenderingAttachmentInfo> attachments, VkRenderingAttachmentInfo? depthAttachment = null,VkRenderingAttachmentInfo? stencilAttachment = null) =>
        BeginRendering(cmd, new VkRect2D()
        {
            offset = new VkOffset2D()
            {
                x = 0,
                y = 0
            },
            extent = extent
        }, attachments, depthAttachment,stencilAttachment);
    
    
    public static VkCommandBuffer EndRendering(this VkCommandBuffer cmd)
    {
        unsafe
        {
            vkCmdEndRendering(cmd);
        }
        return cmd;
    }

    public static VkCommandBuffer BindDescriptorSets(this VkCommandBuffer cmd,VkPipelineBindPoint bindPoint,VkPipelineLayout pipelineLayout,IEnumerable<VkDescriptorSet> sets,uint firstSet = 0)
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
    
    public static VkCommandBuffer BindDescriptorSets(this VkCommandBuffer cmd,VkPipelineBindPoint bindPoint,VkPipelineLayout pipelineLayout,IEnumerable<DescriptorSet> sets,uint firstSet = 0)
    {
        unsafe
        {
            var allSets = sets.Select(c => (VkDescriptorSet)c).ToArray();
            fixed (VkDescriptorSet* pSets = allSets)
            {
                uint numSets = (uint)allSets.Length;
                vkCmdBindDescriptorSets(cmd, bindPoint,
                    pipelineLayout, firstSet,numSets, pSets, 0, null);
            }
        }

        return cmd;
    }

    public static VkCommandBuffer PushConstant<T>(this VkCommandBuffer cmd,VkPipelineLayout pipelineLayout,VkShaderStageFlags stageFlags,T data, uint offset = 0) where T : unmanaged
    {
        unsafe
        {
            vkCmdPushConstants(cmd, pipelineLayout,
                stageFlags, offset,(uint)Marshal.SizeOf<T>(),&data);
        }

        return cmd;
    }
}