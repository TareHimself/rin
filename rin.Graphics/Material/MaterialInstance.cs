// using rin.Graphics.Descriptors;
// using rin.Graphics.Shaders;
// using TerraFX.Interop.Vulkan;
//
// namespace rin.Graphics.Material;
//
// using static Vulkan;
//
// /// <summary>
// ///     Abstracts shaders, pipelines and descriptors
// /// </summary>
// public class MaterialInstance : MultiDisposable
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
//     private readonly CompoundShaderModule _shader;
//
//     private readonly Dictionary<uint, DescriptorSet> _sets = [];
//     
//     public enum ParameterType
//     {
//         Texture,
//         Buffer
//     }
//     
//     public struct Parameter
//     {
//         public string Id;
//         public uint[] Bindings;
//         public DescriptorSet[] Sets;
//     }
//
//     public MaterialInstance(CompoundShaderModule compoundShader)
//     {
//         var subsystem = SGraphicsModule.Get();
//         _shader = compoundShader;
//         foreach (var setLayout in _shader.Layouts)
//         {
//             _sets.Add(setLayout.Key,subsystem.GetDescriptorAllocator().Allocate(setLayout.Value));
//         }
//     }
//
//     public MaterialInstance(IEnumerable<ShaderModule> shaderModules) : this(new CompoundShaderModule(shaderModules))
//     {
//         
//     }
//
//     public MaterialInstance(string filePath) : this(SGraphicsModule.Get().LoadShader(filePath))
//     {
//         
//     }
//
//     protected override void OnDispose(bool isManual)
//     {
//         var subsystem = SRuntime.Get().GetModule<SGraphicsModule>();
//         subsystem.WaitDeviceIdle();
//         var device = subsystem.GetDevice();
//         unsafe
//         {
//             _shader.Dispose();
//             foreach (var set in _sets) set.Value.Dispose();
//             _sets.Clear();
//         }
//     }
//     
//     
//     public IEnumerable<string> GetTextureParameters() => _shader.Parameters.Where(c => c.Value.Type == CompoundShaderModule.ParameterType.Texture).Select(c => c.Key);
//     
//     public IEnumerable<string> GetBufferParameters() => _shader.Parameters.Where(c => c.Value.Type == CompoundShaderModule.ParameterType.Buffer).Select(c => c.Key);
//
//     /// <summary>
//     ///     Binds a <see cref="Texture" /> to this <see cref="MaterialInstance" />
//     /// </summary>
//     public bool BindTexture(string id, Texture texture)
//     {
//         if (!_shader.Parameters.TryGetValue(id, out var resource)) return false;
//
//         var set = _sets[resource.Set];
//         
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
//         if (!_shader.Parameters.TryGetValue(id, out var resource)) return false;
//
//         var set = _sets[resource.Set];
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
//         if (!_shader.Parameters.TryGetValue(id, out var resource)) return false;
//
//         var set = _sets[resource.Set];
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
//         if (!_shader.Parameters.TryGetValue(id, out var resource)) return false;
//
//         var set = _sets[resource.Set];
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
//         if (!_shader.Parameters.TryGetValue(id, out var resource)) return false;
//
//         var set = _sets[resource.Set];
//         set.WriteBuffer(resource.Binding, buffer, type, offset);
//
//         return true;
//     }
//
//     /// <summary>
//     ///     Pushes a constant this <see cref="MaterialInstance" />
//     /// </summary>
//     public bool Push<T>(VkCommandBuffer commandBuffer,T constant,string id = "push")
//     {
//         if (!_shader.PushConstants.TryGetValue(id, out var pushConstant)) return false;
//
//         unsafe
//         {
//             vkCmdPushConstants(commandBuffer, _shader.PipelineLayout, pushConstant.Stages, 0,
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
//         var cmd = frame.GetCommandBuffer();
//         _shader.Bind(frame);
//         var sets = _sets.Select(kv => (VkDescriptorSet)kv.Value).ToArray();
//         unsafe
//         {
//             fixed (VkDescriptorSet* pSets = sets)
//             {
//                 vkCmdBindDescriptorSets(frame.GetCommandBuffer(), VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
//                     _shader.PipelineLayout, 0, (uint)sets.Length, pSets, 0, null);
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
// }