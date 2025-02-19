using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.Shaders;

public interface IShader : IDisposable
{
    public Dictionary<string, Resource> Resources { get; }
    public Dictionary<string, PushConstant> PushConstants { get; }

    public bool Bind(VkCommandBuffer cmd, bool wait = false);
    public void Compile(ICompilationContext context);
    public Dictionary<uint, VkDescriptorSetLayout> GetDescriptorSetLayouts();
    public VkPipelineLayout GetPipelineLayout();
}