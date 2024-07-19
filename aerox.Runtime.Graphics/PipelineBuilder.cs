using aerox.Runtime.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.VkStructureType;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Graphics;

/// <summary>
///     Creates a new pipeline
/// </summary>
public class PipelineBuilder
{
    private readonly List<VkFormat> _colorAttachmentFormats = new();
    public readonly List<ShaderModule> ShaderModules = [];

    private VkPipelineColorBlendAttachmentState _colorBlendAttachment;
    private VkPipelineDepthStencilStateCreateInfo _depthStencil;
    private VkPipelineInputAssemblyStateCreateInfo _inputAssembly;

    private VkPipelineLayout _layout;
    private VkPipelineMultisampleStateCreateInfo _multisampling;
    private VkPipelineRasterizationStateCreateInfo _rasterizer;
    private VkPipelineRenderingCreateInfo _renderInfo;
    private VkPipelineVertexInputStateCreateInfo _vertexInputInfo;

    public PipelineBuilder()
    {
        _vertexInputInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO;
        _inputAssembly.sType = VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO;
        _rasterizer.sType = VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO;
        _multisampling.sType = VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO;
        _depthStencil.sType = VK_STRUCTURE_TYPE_PIPELINE_DEPTH_STENCIL_STATE_CREATE_INFO;
        _renderInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_RENDERING_CREATE_INFO;
    }


    public PipelineBuilder AddShaderModules(params ShaderModule[] modules)
    {
        ShaderModules.AddRange(modules);
        return this;
    }

    public PipelineBuilder SetInputTopology(VkPrimitiveTopology topology)
    {
        _inputAssembly.topology = topology;
        _inputAssembly.primitiveRestartEnable = 0;
        return this;
    }

    public PipelineBuilder SetPolygonMode(VkPolygonMode polygonMode)
    {
        _rasterizer.polygonMode = polygonMode;
        _rasterizer.lineWidth = 1.0f;
        return this;
    }


    public PipelineBuilder SetCullMode(VkCullModeFlags cullMode, VkFrontFace frontFace)
    {
        _rasterizer.cullMode = cullMode;
        _rasterizer.frontFace = frontFace;
        return this;
    }

    public unsafe PipelineBuilder DisableMultisampling()
    {
        _multisampling.sampleShadingEnable = 0;
        _multisampling.rasterizationSamples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT;
        _multisampling.minSampleShading = 1.0f;
        _multisampling.pSampleMask = null;
        _multisampling.alphaToCoverageEnable = 0;
        _multisampling.alphaToOneEnable = 0;
        return this;
    }


    public PipelineBuilder DisableBlending()
    {
        _colorBlendAttachment.colorWriteMask = VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
                                               VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
                                               VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
                                               VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT;
        _colorBlendAttachment.blendEnable = 0;
        return this;
    }


    public PipelineBuilder EnableBlendingAdditive()
    {
        _colorBlendAttachment.colorWriteMask = VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
                                               VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
                                               VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
                                               VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT;
        _colorBlendAttachment.blendEnable = 1;
        _colorBlendAttachment.srcColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE;
        _colorBlendAttachment.dstColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE_MINUS_DST_ALPHA;
        _colorBlendAttachment.colorBlendOp = VkBlendOp.VK_BLEND_OP_ADD;
        _colorBlendAttachment.srcAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE;
        _colorBlendAttachment.dstAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ZERO;
        _colorBlendAttachment.alphaBlendOp = VkBlendOp.VK_BLEND_OP_ADD;
        return this;
    }

    public PipelineBuilder EnableBlendingAlphaBlend()
    {
        _colorBlendAttachment.colorWriteMask = VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
                                               VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
                                               VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
                                               VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT;
        _colorBlendAttachment.blendEnable = 1;
        _colorBlendAttachment.srcColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_SRC_ALPHA;
        _colorBlendAttachment.dstColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA;
        _colorBlendAttachment.colorBlendOp = VkBlendOp.VK_BLEND_OP_ADD;
        _colorBlendAttachment.srcAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_SRC_ALPHA;
        _colorBlendAttachment.dstAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA;
        _colorBlendAttachment.alphaBlendOp = VkBlendOp.VK_BLEND_OP_SUBTRACT;
        return this;
    }

    /// <summary>
    ///     Set's the attachment formats of the resulting <see cref="VkPipeline" />
    /// </summary>
    public PipelineBuilder AddColorAttachment(ImageFormat format)
    {
        _colorAttachmentFormats.Add(SGraphicsModule.ImageFormatToVkFormat(format));
        return this;
    }

    public PipelineBuilder EnableDepthTest(bool depthWriteEnable, VkCompareOp compareOp, VkFormat format)
    {
        _renderInfo.depthAttachmentFormat = format;
        _depthStencil.depthTestEnable = 1;
        _depthStencil.depthWriteEnable = (uint)(depthWriteEnable ? 1 : 0);
        _depthStencil.depthCompareOp = compareOp;
        _depthStencil.depthBoundsTestEnable = 0;
        _depthStencil.front = new VkStencilOpState();
        _depthStencil.back = new VkStencilOpState();
        _depthStencil.minDepthBounds = 0.0f;
        _depthStencil.maxDepthBounds = 1.0f;
        return this;
    }
    
    public PipelineBuilder EnableStencil(VkStencilOpState stencilOpState, VkFormat format)
    {
        _renderInfo.depthAttachmentFormat = format;
        _depthStencil.depthTestEnable = 0;
        _depthStencil.depthWriteEnable = 0;
        _depthStencil.depthCompareOp = VkCompareOp.VK_COMPARE_OP_ALWAYS;
        _depthStencil.depthBoundsTestEnable = 0;
        _depthStencil.stencilTestEnable = 1;
        _depthStencil.front = stencilOpState;
        _depthStencil.back = stencilOpState;
        return this;
    }

    public PipelineBuilder DisableDepthTest()
    {
        _depthStencil.depthTestEnable = 0;
        _depthStencil.depthWriteEnable = 0;
        _depthStencil.depthCompareOp = VkCompareOp.VK_COMPARE_OP_NEVER;
        _depthStencil.depthBoundsTestEnable = 0;
        _depthStencil.stencilTestEnable = 0;
        _depthStencil.front = new VkStencilOpState();
        _depthStencil.back = new VkStencilOpState();
        _depthStencil.minDepthBounds = 0.0f;
        _depthStencil.maxDepthBounds = 1.0f;
        return this;
    }

    public PipelineBuilder SetLayout(VkPipelineLayout layout)
    {
        _layout = layout;
        return this;
    }

    public unsafe VkPipeline Build()
    {
        List<VkPipelineShaderStageCreateInfo> shaderStages = new();

        fixed (byte* str = "main"u8.ToArray())
        {
            foreach (var shaderModule in ShaderModules)
            {
                shaderStages.Add(new VkPipelineShaderStageCreateInfo
                {
                    sType = VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,
                    stage = shaderModule.StageFlags,
                    module = SGraphicsModule.Get().CreateDeviceShaderModule(shaderModule),
                    pName = (sbyte*)str
                });
            }
        }


        var attachments = _colorAttachmentFormats.Select(a => _colorBlendAttachment).ToArray();


        fixed (VkPipelineColorBlendAttachmentState* pAttachments = attachments)
        {
            var colorBlending = new VkPipelineColorBlendStateCreateInfo
            {
                sType = VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO,
                logicOpEnable = 0,
                logicOp = VkLogicOp.VK_LOGIC_OP_COPY,
                pAttachments = pAttachments,
                attachmentCount = (uint)attachments.Length
            };

            VkDynamicState[] state =
                [VkDynamicState.VK_DYNAMIC_STATE_VIEWPORT, VkDynamicState.VK_DYNAMIC_STATE_SCISSOR];

            fixed (VkDynamicState* pDynamicStates = state)
            {
                var dynamicInfo = new VkPipelineDynamicStateCreateInfo
                {
                    sType = VK_STRUCTURE_TYPE_PIPELINE_DYNAMIC_STATE_CREATE_INFO,
                    pDynamicStates = pDynamicStates,
                    dynamicStateCount = (uint)state.Length
                };

                fixed (VkFormat* pColorAttachmentFormats = _colorAttachmentFormats.ToArray())
                {
                    fixed (VkPipelineShaderStageCreateInfo* pStages = shaderStages.ToArray())
                    {
                        fixed (VkPipelineVertexInputStateCreateInfo* pVertexInputState = &_vertexInputInfo)
                        {
                            fixed (VkPipelineInputAssemblyStateCreateInfo* pInputAssemblyState = &_inputAssembly)
                            {
                                fixed (VkPipelineRasterizationStateCreateInfo* pRasterizationState = &_rasterizer)
                                {
                                    fixed (VkPipelineMultisampleStateCreateInfo* pMultisampleState = &_multisampling)
                                    {
                                        fixed (VkPipelineDepthStencilStateCreateInfo* pDepthStencilState =
                                                   &_depthStencil)
                                        {
                                            fixed (VkPipelineRenderingCreateInfo* pRendering = &_renderInfo)
                                            {
                                                _renderInfo.pColorAttachmentFormats = pColorAttachmentFormats;
                                                _renderInfo.colorAttachmentCount = (uint)_colorAttachmentFormats.Count;

                                                var viewportState = new VkPipelineViewportStateCreateInfo
                                                {
                                                    sType = VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO,
                                                    scissorCount = 1,
                                                    viewportCount = 1
                                                };

                                                var pipelineCreateInfo = new VkGraphicsPipelineCreateInfo
                                                {
                                                    sType = VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO,
                                                    pStages = pStages,
                                                    stageCount = (uint)shaderStages.Count,
                                                    pVertexInputState = pVertexInputState,
                                                    pInputAssemblyState = pInputAssemblyState,
                                                    pViewportState = &viewportState,
                                                    pRasterizationState = pRasterizationState,
                                                    pMultisampleState = pMultisampleState,
                                                    pColorBlendState = &colorBlending,
                                                    pDepthStencilState = pDepthStencilState,
                                                    pDynamicState = &dynamicInfo,
                                                    layout = _layout,
                                                    pNext = pRendering
                                                };

                                                var device = SRuntime.Get().GetModule<SGraphicsModule>()
                                                    .GetDevice();
                                                VkPipeline result;
                                                vkCreateGraphicsPipelines(device, new VkPipelineCache(), 1,
                                                    &pipelineCreateInfo, null, &result);

                                                foreach (var shaderStageCreateInfo in shaderStages)
                                                {
                                                    vkDestroyShaderModule(device,shaderStageCreateInfo.module,null);
                                                }
                                                return result;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}