using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Graphics.Shaders;

public class CompiledShader
{
    public readonly List<Pair<VkShaderEXT,VkShaderStageFlags>> Shaders = [];
    public readonly Dictionary<uint, VkDescriptorSetLayout> DescriptorLayouts = [];
    public VkPipelineLayout PipelineLayout;
}