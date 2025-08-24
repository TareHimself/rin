using System.Collections.Frozen;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics.Shaders;

public interface IVulkanShader : IShader
{
    public FrozenDictionary<string, Resource> Resources { get; }
    public FrozenDictionary<string, PushConstant> PushConstants { get; }

    public Dictionary<uint, VkDescriptorSetLayout> GetDescriptorSetLayouts();

    public VkPipelineBindPoint GetBindPoint();
    public VkPipelineLayout GetPipelineLayout();
}