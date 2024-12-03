using rin.Framework.Core;
using rsl.Nodes;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.Shaders;

public interface IShader : IDisposable
{

    public Dictionary<string, Resource> Resources { get; }
    public Dictionary<string, PushConstant> PushConstants { get; }

    public bool Bind(VkCommandBuffer cmd, bool wait = false);
    public CompiledShader Compile(IShaderCompiler compiler);
    
    public void Init();
    public Dictionary<uint, VkDescriptorSetLayout> GetDescriptorSetLayouts();
    public VkPipelineLayout GetPipelineLayout();
}