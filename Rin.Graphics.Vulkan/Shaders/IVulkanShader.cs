using System.Collections.Frozen;
using Rin.Core.Graphics.Shaders;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Shaders;

public interface IVulkanShader : IShader
{
    public FrozenDictionary<string, Resource> Resources { get; }
    public FrozenDictionary<string, PushConstant> PushConstants { get; }

    public Dictionary<uint, VkDescriptorSetLayout> GetDescriptorSetLayouts();

    public VkPipelineBindPoint GetBindPoint();
    public VkPipelineLayout GetPipelineLayout();
}