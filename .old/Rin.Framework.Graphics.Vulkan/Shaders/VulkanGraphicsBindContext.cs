using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Rin.Framework.Graphics.Shaders;
using Rin.Framework.Graphics.Vulkan.Shaders.Slang;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Framework.Graphics.Vulkan.Shaders;

public class VulkanGraphicsBindContext(SlangGraphicsShader shader, VulkanExecutionContext executionContext)
    : VulkanBindContext<IGraphicsBindContext>(executionContext), IGraphicsBindContext
{
    protected override FrozenDictionary<string, Resource> Resources => shader.Resources;
    protected override FrozenDictionary<string, PushConstant> PushConstants => shader.PushConstants;

    public override IGraphicsBindContext Push<T>(in T data, uint offset = 0)
    {
        var cmd = ExecutionContext.CommandBuffer;
        unsafe
        {
            var layout = shader.GetPipelineLayout();
            const VkShaderStageFlags flags = VkShaderStageFlags.VK_SHADER_STAGE_COMPUTE_BIT |
                                             VkShaderStageFlags.VK_SHADER_STAGE_ALL_GRAPHICS;
            fixed (T* pData = &data)
            {
                vkCmdPushConstants(cmd, layout, flags, offset,
                    (uint)Utils.ByteSizeOf<T>(), pData);
                // var size = (uint)Utils.ByteSizeOf<T>();
                // if (size < 256)
                // {
                //     var diff = 256 - size;
                //     var padding = stackalloc byte[(int)diff];
                //     vkCmdPushConstants(cmd, GetPipelineLayout(), flags, size, diff, padding);
                // }
            }
        }

        return this;
    }
        
    public IGraphicsShader Shader => shader;

    public IGraphicsBindContext Draw(uint vertices, uint instances = 1, uint firstVertex = 0, uint firstInstance = 0)
    {
        UpdatePendingSets();
        
        vkCmdDraw(ExecutionContext.CommandBuffer, vertices, instances, firstVertex, firstInstance);
        return this;
    }

    public IGraphicsBindContext DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0,
        uint firstVertex = 0,
        uint firstInstance = 0)
    {
        UpdatePendingSets();
        
        vkCmdDrawIndexed(ExecutionContext.CommandBuffer, indexCount, instanceCount, firstIndex, (int)firstVertex,
            firstInstance);
        
        return this;
    }

    public IGraphicsBindContext DrawIndexedIndirect(in DeviceBufferView commands, uint drawCount, uint stride,
        uint commandsOffset = 0)
    {
        Debug.Assert(commands.IsValid, "Indirect command buffer is not valid");
        Debug.Assert(commands.Buffer is IVulkanDeviceBuffer);
        UpdatePendingSets();
        
        vkCmdDrawIndexedIndirect(ExecutionContext.CommandBuffer,Unsafe.As<IVulkanDeviceBuffer>(commands.Buffer).NativeBuffer, commands.Offset,
            drawCount, stride);
        
        return this;
    }

    public IGraphicsBindContext DrawIndexedIndirectCount(in DeviceBufferView commands, in DeviceBufferView drawCount,
        uint maxDrawCount, uint stride, uint commandsOffset = 0, uint drawCountOffset = 0)
    {
        Debug.Assert(commands.IsValid, "Indirect command buffer is not valid");
        
        Debug.Assert(drawCount.IsValid, "Draw count buffer is not valid");
        
        Debug.Assert(commands.Buffer is IVulkanDeviceBuffer);
        Debug.Assert(drawCount.Buffer is IVulkanDeviceBuffer);
        UpdatePendingSets();
        
        vkCmdDrawIndexedIndirectCount(ExecutionContext.CommandBuffer,Unsafe.As<IVulkanDeviceBuffer>(commands.Buffer).NativeBuffer, commands.Offset,
            Unsafe.As<IVulkanDeviceBuffer>(drawCount.Buffer).NativeBuffer,
            drawCount.Offset, maxDrawCount, (uint)Utils.ByteSizeOf<VkDrawIndexedIndirectCommand>());
        
        return this;
    }

    protected override IShader GetShader()
    {
        return shader;
    }

    protected override IGraphicsBindContext GetInterface()
    {
        return this;
    }
}