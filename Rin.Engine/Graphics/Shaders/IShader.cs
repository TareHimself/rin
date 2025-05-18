using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Graphics.Shaders;

public interface IShader : IDisposable
{
    public Dictionary<string, Resource> Resources { get; }
    public Dictionary<string, PushConstant> PushConstants { get; }
    public bool Ready { get; }
    public bool Bind(in VkCommandBuffer cmd, bool wait = true);
    public void Compile(ICompilationContext context);
    public Dictionary<uint, VkDescriptorSetLayout> GetDescriptorSetLayouts();
    public VkPipelineLayout GetPipelineLayout();
    public VkShaderStageFlags GetStageFlags();

    public void Push<T>(in VkCommandBuffer cmd, in T data, uint offset = 0) where T : unmanaged
    {
        unsafe
        {
            var flags = VkShaderStageFlags.VK_SHADER_STAGE_ALL_GRAPHICS |
                        VkShaderStageFlags.VK_SHADER_STAGE_COMPUTE_BIT;
            fixed (T* pData = &data)
            {
                var size = (uint)Utils.ByteSizeOf<T>();
                vkCmdPushConstants(cmd, GetPipelineLayout(), flags, offset,
                    (uint)Utils.ByteSizeOf<T>(), pData);
                if (size < 256)
                {
                    var diff = 256 - size;
                    var padding = stackalloc byte[(int)diff];
                    vkCmdPushConstants(cmd, GetPipelineLayout(), flags, size, diff, padding);
                }
            }
        }
    }
}