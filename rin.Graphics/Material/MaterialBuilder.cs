// using rin.Graphics.Descriptors;
// using rin.Graphics.Shaders;
// using TerraFX.Interop.Vulkan;
//
// namespace rin.Graphics.Material;
//
// using static Vulkan;
//
// /// <summary>
// ///     Builds a new <see cref="MaterialInstance" />
// /// </summary>
// public class MaterialBuilder
// {
//     public readonly List<ShaderModule> ShaderModules = [];
//     public readonly PipelineBuilder Pipeline = new();
//     private Func<PipelineBuilder, PipelineBuilder> _configurePipelineFn = a => a;
//
//     /// <summary>
//     ///     Add a shader to the resulting <see cref="MaterialInstance" />
//     /// </summary>
//     public MaterialBuilder AddShaderModules(params ShaderModule[] shaders)
//     {
//         ShaderModules.AddRange(shaders);
//         return this;
//     }
//
//     public MaterialBuilder AddAttachmentFormats(params ImageFormat[] formats)
//     {
//         return this;
//     }
//
//     /// <summary>
//     ///     Builds a new <see cref="MaterialInstance" />
//     /// </summary>
//     public MaterialInstance Build()
//     {
//         var subsystem = SRuntime.Get().GetModule<SGraphicsModule>();
//
//         return new MaterialInstance(new CompoundShaderModule(ShaderModules));
//         
//         // Dictionary<MaterialInstance.SetType, DescriptorLayoutBuilder> layoutBuilders = new();
//         //
//         // uint maxLayout = 0;
//         // MaterialResources resources = new();
//         // foreach (var shaderModule in Pipeline.ShaderModules)
//         // {
//         //     foreach (var texture in shaderModule.Textures)
//         //     {
//         //
//         //         resources.Textures.TryAdd(texture.Key, texture.Value);
//         //         if (!layoutBuilders.ContainsKey((MaterialInstance.SetType)texture.Value.Set))
//         //         {
//         //             layoutBuilders.Add((MaterialInstance.SetType)texture.Value.Set, new DescriptorLayoutBuilder());
//         //
//         //             if (maxLayout < texture.Value.Set) maxLayout = texture.Value.Set;
//         //         }
//         //
//         //         layoutBuilders[(MaterialInstance.SetType)texture.Value.Set].AddBinding(texture.Value.Binding,
//         //             VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, shaderModule.StageFlags, texture.Value.Count);
//         //     }
//         //
//         //
//         //     foreach (var buffer in shaderModule.Buffers)
//         //     {
//         //         resources.Buffers.TryAdd(buffer.Key, buffer.Value);
//         //         if (!layoutBuilders.ContainsKey((MaterialInstance.SetType)buffer.Value.Set))
//         //         {
//         //             layoutBuilders.Add((MaterialInstance.SetType)buffer.Value.Set, new DescriptorLayoutBuilder());
//         //
//         //             if (maxLayout < buffer.Value.Set) maxLayout = buffer.Value.Set;
//         //         }
//         //
//         //         layoutBuilders[(MaterialInstance.SetType)buffer.Value.Set].AddBinding(buffer.Value.Binding,
//         //             VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER, shaderModule.StageFlags, buffer.Value.Count);
//         //     }
//         //     
//         //     foreach (var pushConstant in shaderModule.PushConstants)
//         //     {
//         //         var newPush = new ShaderModule.PushConstant()
//         //         {
//         //             Name = pushConstant.Value.Name,
//         //             Size = pushConstant.Value.Size,
//         //             Stages = pushConstant.Value.Stages
//         //         };
//         //         
//         //         if (!resources.PushConstants.TryAdd(pushConstant.Key,newPush))
//         //         {
//         //             newPush.Stages |= resources.PushConstants[pushConstant.Key].Stages;
//         //             resources.PushConstants[pushConstant.Key] = newPush;
//         //         }
//         //     }
//         // }
//         //
//         // VkPushConstantRange[] pushConstantRanges = resources.PushConstants.Select(c => new VkPushConstantRange()
//         // {
//         //     stageFlags = c.Value.Stages,
//         //     offset = 0,
//         //     size = (uint)c.Value.Size
//         // }).ToArray();
//         //
//         // Dictionary<MaterialInstance.SetType, VkDescriptorSetLayout> layouts = new();
//         //
//         // var layoutsList = new VkDescriptorSetLayout[maxLayout + 1];
//         //
//         // for (var i = 0; i < maxLayout + 1; i++)
//         // {
//         //     var layoutType = (MaterialInstance.SetType)i;
//         //     var layout = layoutBuilders.TryGetValue(layoutType, out var builder)
//         //         ? builder.Build()
//         //         : new DescriptorLayoutBuilder().Build();
//         //
//         //     layouts.Add(layoutType, layout);
//         //     layoutsList[i] = layout;
//         // }
//         //
//         // unsafe
//         // {
//         //     fixed (VkDescriptorSetLayout* pLayouts = layoutsList.ToArray())
//         //     {
//         //         fixed (VkPushConstantRange* pRanges = pushConstantRanges)
//         //         {
//         //             var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo
//         //             {
//         //                 sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO,
//         //                 pSetLayouts = pLayouts,
//         //                 setLayoutCount = (uint)layoutsList.Length,
//         //                 pPushConstantRanges = pRanges,
//         //                 pushConstantRangeCount = (uint)pushConstantRanges.Length
//         //             };
//         //
//         //
//         //             VkPipelineLayout newLayout;
//         //             vkCreatePipelineLayout(subsystem.GetDevice(), &pipelineLayoutInfo, null, &newLayout);
//         //
//         //             Pipeline
//         //                 .SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
//         //                 .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
//         //                 .DisableMultisampling()
//         //                 .SetLayout(newLayout);
//         //
//         //             // if (_type == MaterialInstance.Type.Widget)
//         //             // {
//         //             //     _pipelineBuilder
//         //             //         .DisableDepthTest()
//         //             //         .EnableBlendingAlphaBlend();
//         //             // }
//         //             // else
//         //             // {
//         //             //     _pipelineBuilder
//         //             //         .SetCullMode(VkCullModeFlags.VK_CULL_MODE_NONE, VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
//         //             //         .DisableBlending()
//         //             //         .EnableDepthTest(true, VkCompareOp.VK_COMPARE_OP_LESS_OR_EQUAL,
//         //             //             VkFormat.VK_FORMAT_D32_SFLOAT);
//         //             //
//         //             //     if (_type == MaterialInstance.Type.Translucent)
//         //             //         _pipelineBuilder
//         //             //             .EnableBlendingAdditive()
//         //             //             .EnableDepthTest(false, VkCompareOp.VK_COMPARE_OP_LESS_OR_EQUAL,
//         //             //                 VkFormat.VK_FORMAT_D32_SFLOAT);
//         //             // }
//         //
//         //             var newPipeline = Pipeline.Build();
//         //
//         //             var globalAllocator = subsystem.GetDescriptorAllocator();
//         //
//         //
//         //             var sets = layouts.Where(layout => layout.Key != MaterialInstance.SetType.Frame)
//         //                 .ToDictionary(layout => layout.Key, layout => globalAllocator.Allocate(layout.Value));
//         //
//         //             return new MaterialInstance(resources, newLayout, newPipeline, sets, layouts);
//         //         }
//         //     }
//         // }
//     }
//
//     
// }