using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace Rin.Engine.Graphics.Shaders;

public interface IShader : IDisposable
{
    public Dictionary<string, Resource> Resources { get; }
    public Dictionary<string, PushConstant> PushConstants { get; }
    public bool Ready { get; }
    public bool Bind(in VkCommandBuffer cmd, bool wait = false);
    public void Compile(ICompilationContext context);
    public Dictionary<uint, VkDescriptorSetLayout> GetDescriptorSetLayouts();
    public VkPipelineLayout GetPipelineLayout();
    public VkShaderStageFlags GetStageFlags();
    public void Push<T>(in VkCommandBuffer cmd, in T data,uint offset = 0) where T : unmanaged
    {
        unsafe
        {
            fixed (T* pData = &data)
            {
                vkCmdPushConstants(cmd,GetPipelineLayout(),GetStageFlags(),offset,(uint)Engine.Utils.ByteSizeOf<T>(),pData);
            }
        }
    }
}