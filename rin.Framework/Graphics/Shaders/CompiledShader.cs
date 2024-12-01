using rin.Framework.Core;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.Shaders;

public class CompiledShader
{
    public readonly List<Pair<VkShaderEXT,VkShaderStageFlags>> Shaders = [];
    public readonly Dictionary<uint, VkDescriptorSetLayout> DescriptorLayouts = [];
    public VkPipelineLayout PipelineLayout;
}