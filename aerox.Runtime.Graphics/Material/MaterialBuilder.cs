using aerox.Runtime.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Graphics.Material;

using static Vulkan;

/// <summary>
///     Builds a new <see cref="MaterialInstance" />
/// </summary>
public class MaterialBuilder
{
    private Shader? _shader = null;
    public readonly PipelineBuilder Pipeline = new();
    private Func<PipelineBuilder, PipelineBuilder> _configurePipelineFn = a => a;

    /// <summary>
    ///     Add a shader to the resulting <see cref="MaterialInstance" />
    /// </summary>
    public MaterialBuilder SetShader(Shader shader)
    {
        _shader = shader;
        Pipeline.SetShader(shader);
        return this;
    }

    public MaterialBuilder AddAttachmentFormats(params EImageFormat[] formats)
    {
        foreach (var format in formats) Pipeline.AddColorAttachment(format);

        return this;
    }

    /// <summary>
    ///     Builds a new <see cref="MaterialInstance" />
    /// </summary>
    public MaterialInstance Build()
    {
        var subsystem = SRuntime.Get().GetModule<SGraphicsModule>();
        
        Dictionary<MaterialInstance.SetType, DescriptorLayoutBuilder> layoutBuilders = new();

        uint maxLayout = 0;
        Shader.Stage.Resources resources = new();
        foreach (var stage in _shader?.GetStages() ?? [])
        {
            var stageResources = stage.GetResources();
            foreach (var texture in stageResources.Textures)
            {

                resources.Textures.TryAdd(texture.Key, texture.Value);
                if (!layoutBuilders.ContainsKey((MaterialInstance.SetType)texture.Value.Set))
                {
                    layoutBuilders.Add((MaterialInstance.SetType)texture.Value.Set, new DescriptorLayoutBuilder());

                    if (maxLayout < texture.Value.Set) maxLayout = texture.Value.Set;
                }

                layoutBuilders[(MaterialInstance.SetType)texture.Value.Set].AddBinding(texture.Value.Binding,
                    VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, stage.Flags, texture.Value.Count);
            }


            foreach (var buffer in stageResources.Buffers)
            {
                resources.Buffers.TryAdd(buffer.Key, buffer.Value);
                if (!layoutBuilders.ContainsKey((MaterialInstance.SetType)buffer.Value.Set))
                {
                    layoutBuilders.Add((MaterialInstance.SetType)buffer.Value.Set, new DescriptorLayoutBuilder());

                    if (maxLayout < buffer.Value.Set) maxLayout = buffer.Value.Set;
                }

                layoutBuilders[(MaterialInstance.SetType)buffer.Value.Set].AddBinding(buffer.Value.Binding,
                    VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER, stage.Flags, buffer.Value.Count);
            }

            resources.Push = stageResources.Push == null
                ? resources.Push
                : (resources.Push == null
                    ? new Shader.Stage.PushConstant()
                    {
                        Name = stageResources.Push.Name,
                        Size = stageResources.Push.Size,
                        Stages = stageResources.Push.Stages
                    }
                    : new Shader.Stage.PushConstant()
                    {
                        Name = resources.Push.Name,
                        Size = resources.Push.Size,
                        Stages = resources.Push.Stages | stageResources.Push.Stages
                    });
        }

        VkPushConstantRange[] pushConstantRanges = resources.Push == null ? [] : [new VkPushConstantRange()
        {
            stageFlags = resources.Push.Stages,
            offset = 0,
            size = (uint)resources.Push.Size
        }];

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

                    return new MaterialInstance(resources, newLayout, newPipeline, sets, layouts);
                }
            }
        }
    }
}