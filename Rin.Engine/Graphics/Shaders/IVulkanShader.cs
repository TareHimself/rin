using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.Shaders;

public interface IVulkanShader : IShader
{
    public Dictionary<uint, VkDescriptorSetLayout> GetDescriptorSetLayouts();
    
    public VkPipelineBindPoint GetBindPoint();
    public VkPipelineLayout GetPipelineLayout();
    public VkShaderStageFlags GetStageFlags();
}