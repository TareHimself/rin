using aerox.Runtime.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Graphics.Material;

using static Vulkan;

/// <summary>
///     Builds a new <see cref="MaterialInstance" />
/// </summary>
public class MaterialBuilder
{
    private readonly List<Shader> _shaders = new();
    public readonly PipelineBuilder Pipeline = new();
    private Func<PipelineBuilder, PipelineBuilder> _configurePipelineFn = a => a;

    /// <summary>
    ///     Add a shader to the resulting <see cref="MaterialInstance" />
    /// </summary>
    public MaterialBuilder AddShader(Shader shader)
    {
        _shaders.Add(shader);
        Pipeline.AddShader(shader);
        return this;
    }

    public MaterialBuilder AddAttachmentFormats(params VkFormat[] formats)
    {
        foreach (var format in formats) Pipeline.AddColorAttachment(format);

        return this;
    }

    /// <summary>
    ///     Adds shaders to the resulting <see cref="MaterialInstance" />
    /// </summary>
    public MaterialBuilder AddShaders(IEnumerable<Shader> shaders)
    {
        foreach (var shader in shaders) AddShader(shader);

        return this;
    }

    private static VkPushConstantRange[] ComputePushConstantRanges(ShaderResources resources)
    {
        List<VkPushConstantRange> ranges = new();

        foreach (var kv in resources.PushConstants)
            ranges.Add(new VkPushConstantRange
            {
                stageFlags = kv.Value.Flags,
                offset = kv.Value.Offset,
                size = kv.Value.Size
            });

        return ranges.ToArray();
    }

    /// <summary>
    ///     Builds a new <see cref="MaterialInstance" />
    /// </summary>
    public MaterialInstance Build()
    {
        var subsystem = SRuntime.Get().GetModule<SGraphicsModule>();

        ShaderResources allShaderResources = new();

        Dictionary<MaterialInstance.SetType, DescriptorLayoutBuilder> layoutBuilders = new();

        uint maxLayout = 0;

        foreach (var shader in _shaders)
        {
            var resources = shader.Resources;
            var stageFlags = shader.GetStageFlags();

            foreach (var kv in resources.Images)
            {
                if (!allShaderResources.Images.TryAdd(kv.Key, kv.Value))
                {
                    var r = allShaderResources.Images[kv.Key];
                    r.Flags |= kv.Value.Flags;
                    allShaderResources.Images[kv.Key] = r;
                }

                if (!layoutBuilders.ContainsKey((MaterialInstance.SetType)kv.Value.Set))
                {
                    layoutBuilders.Add((MaterialInstance.SetType)kv.Value.Set, new DescriptorLayoutBuilder());

                    if (maxLayout < kv.Value.Set) maxLayout = kv.Value.Set;
                }

                layoutBuilders[(MaterialInstance.SetType)kv.Value.Set].AddBinding(kv.Value.Binding,
                    VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, stageFlags, kv.Value.Count);
            }


            foreach (var kv in resources.Buffers)
            {
                if (!allShaderResources.Buffers.TryAdd(kv.Key, kv.Value))
                {
                    var r = allShaderResources.Buffers[kv.Key];
                    r.Flags |= kv.Value.Flags;
                    allShaderResources.Buffers[kv.Key] = r;
                }

                if (!layoutBuilders.ContainsKey((MaterialInstance.SetType)kv.Value.Set))
                {
                    layoutBuilders.Add((MaterialInstance.SetType)kv.Value.Set, new DescriptorLayoutBuilder());

                    if (maxLayout < kv.Value.Set) maxLayout = kv.Value.Set;
                }

                layoutBuilders[(MaterialInstance.SetType)kv.Value.Set].AddBinding(kv.Value.Binding,
                    VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER, stageFlags, kv.Value.Count);
            }

            foreach (var kv in resources.PushConstants)
                if (!allShaderResources.PushConstants.TryAdd(kv.Key, kv.Value))
                {
                    var r = allShaderResources.PushConstants[kv.Key];
                    r.Flags |= kv.Value.Flags;
                    allShaderResources.PushConstants[kv.Key] = r;
                }
        }

        var pushConstantRanges = ComputePushConstantRanges(allShaderResources);

        Dictionary<MaterialInstance.SetType, VkDescriptorSetLayout> layouts = new();

        var layoutsList = new VkDescriptorSetLayout[maxLayout + 1];

        for (var i = 0; i < maxLayout + 1; i++)
        {
            var layoutType = (MaterialInstance.SetType)i;
            var layout = layoutBuilders.TryGetValue(layoutType, out var builder)
                ? builder.Build()
                : new DescriptorLayoutBuilder().Build();

            layouts.Add(layoutType, layout);
            layoutsList[i] = layout;
        }

        unsafe
        {
            fixed (VkDescriptorSetLayout* pLayouts = layoutsList.ToArray())
            {
                fixed (VkPushConstantRange* pRanges = pushConstantRanges)
                {
                    var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo
                    {
                        sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO,
                        pSetLayouts = pLayouts,
                        setLayoutCount = (uint)layoutsList.Length,
                        pPushConstantRanges = pRanges,
                        pushConstantRangeCount = (uint)pushConstantRanges.Length
                    };


                    VkPipelineLayout newLayout;
                    vkCreatePipelineLayout(subsystem.GetDevice(), &pipelineLayoutInfo, null, &newLayout);

                    Pipeline
                        .SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
                        .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
                        .DisableMultisampling()
                        .SetLayout(newLayout);

                    // if (_type == MaterialInstance.Type.Widget)
                    // {
                    //     _pipelineBuilder
                    //         .DisableDepthTest()
                    //         .EnableBlendingAlphaBlend();
                    // }
                    // else
                    // {
                    //     _pipelineBuilder
                    //         .SetCullMode(VkCullModeFlags.VK_CULL_MODE_NONE, VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
                    //         .DisableBlending()
                    //         .EnableDepthTest(true, VkCompareOp.VK_COMPARE_OP_LESS_OR_EQUAL,
                    //             VkFormat.VK_FORMAT_D32_SFLOAT);
                    //
                    //     if (_type == MaterialInstance.Type.Translucent)
                    //         _pipelineBuilder
                    //             .EnableBlendingAdditive()
                    //             .EnableDepthTest(false, VkCompareOp.VK_COMPARE_OP_LESS_OR_EQUAL,
                    //                 VkFormat.VK_FORMAT_D32_SFLOAT);
                    // }

                    var newPipeline = Pipeline.Build();

                    var globalAllocator = subsystem.GetDescriptorAllocator();


                    var sets = layouts.Where(layout => layout.Key != MaterialInstance.SetType.Frame)
                        .ToDictionary(layout => layout.Key, layout => globalAllocator.Allocate(layout.Value));

                    return new MaterialInstance(allShaderResources, newLayout, newPipeline, sets, layouts);
                }
            }
        }
    }
}