using System.Collections.Frozen;
using System.Diagnostics;
using Rin.Framework.Graphics.Shaders.Slang;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Framework.Graphics.Shaders;

public class VulkanComputeBindContext : VulkanBindContext<IComputeBindContext>, IComputeBindContext
{
    private readonly SlangComputeShader _shader;

    public VulkanComputeBindContext(SlangComputeShader shader, VulkanExecutionContext executionContext) : base(
        executionContext)
    {
        _shader = shader;
    }

    protected override FrozenDictionary<string, Resource> Resources => _shader.Resources;
    protected override FrozenDictionary<string, PushConstant> PushConstants => _shader.PushConstants;


    public override IComputeBindContext Push<T>(in T data, uint offset = 0)
    {
        var cmd = ExecutionContext.CommandBuffer;
        unsafe
        {
            var layout = _shader.GetPipelineLayout();
            const VkShaderStageFlags flags = VkShaderStageFlags.VK_SHADER_STAGE_COMPUTE_BIT |
                                             VkShaderStageFlags.VK_SHADER_STAGE_ALL_GRAPHICS;
            fixed (T* pData = &data)
            {
                vkCmdPushConstants(cmd, layout, flags, offset,
                    (uint)Utils.ByteSizeOf<T>(), pData);
            }
        }

        return this;
    }

    public IComputeShader Shader => _shader;

    public IBindContext Dispatch(uint x, uint y = 1, uint z = 1)
    {
        Debug.Assert(x != 0 && y != 0 && z != 0);
        
        UpdatePendingSets();
        
        var cmd = ExecutionContext.CommandBuffer;
        
        vkCmdDispatch(cmd, x, y, z);
        
        return this;
    }

    protected override IShader GetShader()
    {
        return Shader;
    }

    protected override IComputeBindContext GetInterface()
    {
        return this;
    }
}