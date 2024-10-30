// using rin.Graphics.Descriptors;
// using TerraFX.Interop.Vulkan;
//
// namespace rin.Graphics.Material;
//
// using static Vulkan;
//
// /// <summary>
// ///     Abstracts shaders, pipelines and descriptors
// /// </summary>
// public class MaterialInstanceOld : MultiDisposable
// {
//     /// <summary>
//     ///     The type of descriptor set in a material
//     /// </summary>
//     public enum SetType
//     {
//         Runtime,
//         Global,
//         Static,
//         Dynamic,
//         Frame
//     }
//
//     // /// <summary>
//     // ///     The type of this material
//     // /// </summary>
//     // public enum Type
//     // {
//     //     Unknown,
//     //     Lit,
//     //     Unlit,
//     //     Translucent,
//     //     Widget,
//     //     Compute
//     // }
//
//     private readonly VkPipeline _pipeline;
//     private readonly VkPipelineLayout _pipelineLayout;
//     private readonly MaterialResources _resources;
//     private readonly Dictionary<SetType, VkDescriptorSetLayout> _setLayouts;
//     private readonly Dictionary<SetType, DescriptorSet> _sets;
//
//     public MaterialInstanceOld(MaterialResources inResources, VkPipelineLayout inPipelineLayout,
//         VkPipeline inPipeline, Dictionary<SetType, DescriptorSet> inSets,
//         Dictionary<SetType, VkDescriptorSetLayout> inSetLayouts)
//     {
//         _resources = inResources;
//         _pipelineLayout = inPipelineLayout;
//         _pipeline = inPipeline;
//         _sets = inSets;
//         _setLayouts = inSetLayouts;
//     }
//
//     protected override void OnDispose(bool isManual)
//     {
//         var subsystem = SRuntime.Get().GetModule<SGraphicsModule>();
//         subsystem.WaitDeviceIdle();
//         var device = subsystem.GetDevice();
//         unsafe
//         {
//             vkDestroyPipeline(device, _pipeline, null);
//             vkDestroyPipelineLayout(device, _pipelineLayout, null);
//             foreach (var kv in _setLayouts) vkDestroyDescriptorSetLayout(device, kv.Value, null);
//             _setLayouts.Clear();
//             foreach (var set in _sets) set.Value.Dispose();
//             _sets.Clear();
//         }
//     }
//
//     public IEnumerable<string> GetTextureParameters() => _resources.Textures.Select(c => c.Key);
//
//     /// <summary>
//     ///     Binds a <see cref="Texture" /> to this <see cref="MaterialInstance" />
//     /// </summary>
//     public bool BindTexture(string id, Texture texture)
//     {
//         if (!_resources.Textures.TryGetValue(id, out var resource)) return false;
//
//         var set = _sets[(SetType)resource.Set];
//         set.WriteTexture(resource.Binding, texture, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
//
//         return true;
//     }
//
//     /// <summary>
//     ///     Binds a <see cref="Texture" /> array to this <see cref="MaterialInstance" />
//     /// </summary>
//     public bool BindTextureArray(string id, Texture[] textures)
//     {
//         if (!_resources.Textures.TryGetValue(id, out var resource)) return false;
//
//         var set = _sets[(SetType)resource.Set];
//         set.WriteTextures(resource.Binding, textures, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
//
//         return true;
//     }
//
//     /// <summary>
//     ///     Binds an <see cref="Image" /> to this <see cref="MaterialInstance" />
//     /// </summary>
//     public bool BindImage(string id, DeviceImage image, DescriptorSet.ImageType type, VkSampler sampler)
//     {
//         if (!_resources.Textures.TryGetValue(id, out var resource)) return false;
//
//         var set = _sets[(SetType)resource.Set];
//         set.WriteImage(resource.Binding, image, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL, type, sampler);
//
//         return true;
//     }
//
//
//     /// <summary>
//     ///     Binds an <see cref="Image" /> array to this <see cref="MaterialInstance" />
//     /// </summary>
//     public bool BindImageArray(string id, DeviceImage[] images, DescriptorSet.ImageType type, VkSampler sampler)
//     {
//         if (!_resources.Textures.TryGetValue(id, out var resource)) return false;
//
//         var set = _sets[(SetType)resource.Set];
//         set.WriteImages(resource.Binding, images, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL, type,
//             sampler);
//
//         return true;
//     }
//
//     /// <summary>
//     ///     Binds an <see cref="Buffer" /> to this <see cref="MaterialInstance" />
//     /// </summary>
//     public bool BindBuffer(string id, DeviceBuffer buffer,
//         DescriptorSet.BufferType type = DescriptorSet.BufferType.Uniform, ulong offset = 0)
//     {
//         if (!_resources.Buffers.TryGetValue(id, out var resource)) return false;
//
//         var set = _sets[(SetType)resource.Set];
//         set.WriteBuffer(resource.Binding, buffer, type, offset);
//
//         return true;
//     }
//
//     /// <summary>
//     ///     Pushes a constant this <see cref="MaterialInstance" />
//     /// </summary>
//     public bool Push<T>(VkCommandBuffer commandBuffer, T constant, string id = "push")
//     {
//         if (!_resources.PushConstants.TryGetValue(id, out var pushConstant)) return false;
//
//         unsafe
//         {
//             vkCmdPushConstants(commandBuffer, _pipelineLayout, pushConstant.Stages, 0,
//                 (uint)pushConstant.Size, &constant);
//             return true;
//         }
//     }
//
//     /// <summary>
//     ///     Binds this <see cref="MaterialInstance" /> to a <see cref="Frame" />
//     /// </summary>
//     public void BindTo(Frame frame)
//     {
//         vkCmdBindPipeline(frame.GetCommandBuffer(), VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, _pipeline);
//         var sets = _sets.Select(kv => (VkDescriptorSet)kv.Value).ToArray();
//         unsafe
//         {
//             fixed (VkDescriptorSet* pSets = sets)
//             {
//                 vkCmdBindDescriptorSets(frame.GetCommandBuffer(), VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
//                     _pipelineLayout, 0, (uint)sets.Length, pSets, 0, null);
//             }
//         }
//     }
//
//     [Serializable]
//     public class UnknownParameterException : Exception
//     {
//         public UnknownParameterException()
//         {
//         }
//
//         public UnknownParameterException(string parameter)
//             : base($"Parameter [{parameter}] does not exist in material")
//         {
//         }
//     }
//
// }
