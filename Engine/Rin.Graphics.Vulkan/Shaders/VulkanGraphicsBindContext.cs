using System.Collections.Frozen;
using System.Diagnostics;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;
using Rin.Graphics.Vulkan.Shaders.Compiled;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Graphics.Vulkan.Shaders;

public class VulkanGraphicsBindContext(CompiledGraphicsShader shader, VulkanExecutionContext executionContext)
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

    public IGraphicsBindContext DrawIndirect(in DeviceBufferView commands, uint drawCount, uint stride,
        uint commandsOffset = 0)
    {
        var vulkanCommands = ResolveIndirectBuffer(commands, "Indirect command buffer");
        UpdatePendingSets();

        vkCmdDrawIndirect(ExecutionContext.CommandBuffer,
            vulkanCommands.NativeBuffer, commands.Offset + commandsOffset,
            drawCount, stride);

        return this;
    }

    public IGraphicsBindContext DrawIndirectCount(in DeviceBufferView commands, in DeviceBufferView drawCount,
        uint maxDrawCount, uint stride, uint commandsOffset = 0, uint drawCountOffset = 0)
    {
        var vulkanCommands = ResolveIndirectBuffer(commands, "Indirect command buffer");
        var vulkanDrawCount = ResolveIndirectBuffer(drawCount, "Draw count buffer");
        UpdatePendingSets();

        vkCmdDrawIndirectCount(ExecutionContext.CommandBuffer,
            vulkanCommands.NativeBuffer, commands.Offset + commandsOffset,
            vulkanDrawCount.NativeBuffer, drawCount.Offset + drawCountOffset,
            maxDrawCount, stride);

        return this;
    }

    public IGraphicsBindContext DrawIndexedIndirect(in DeviceBufferView commands, uint drawCount, uint stride,
        uint commandsOffset = 0)
    {
        var vulkanCommands = ResolveIndirectBuffer(commands, "Indirect command buffer");
        UpdatePendingSets();

        vkCmdDrawIndexedIndirect(ExecutionContext.CommandBuffer,
            vulkanCommands.NativeBuffer, commands.Offset + commandsOffset,
            drawCount, stride);

        return this;
    }

    public IGraphicsBindContext DrawIndexedIndirectCount(in DeviceBufferView commands, in DeviceBufferView drawCount,
        uint maxDrawCount, uint stride, uint commandsOffset = 0, uint drawCountOffset = 0)
    {
        var vulkanCommands = ResolveIndirectBuffer(commands, "Indirect command buffer");
        var vulkanDrawCount = ResolveIndirectBuffer(drawCount, "Draw count buffer");
        UpdatePendingSets();

        vkCmdDrawIndexedIndirectCount(ExecutionContext.CommandBuffer,
            vulkanCommands.NativeBuffer, commands.Offset + commandsOffset,
            vulkanDrawCount.NativeBuffer, drawCount.Offset + drawCountOffset,
            maxDrawCount, stride);

        return this;
    }

    private static IVulkanDeviceBuffer ResolveIndirectBuffer(in DeviceBufferView view, string role)
    {
        var module = VulkanGraphicsModule.Get();
        Debug.Assert(module.CurrentDevice.SupportsIndirectRendering, "The device does not support indirect rendering");
        Debug.Assert(view.IsValid, $"{role} is not valid");
        var buffer = module.ResolveBuffer(view.Buffer);
        Debug.Assert(buffer is not null, $"{role} handle is not resolvable");
        return buffer!;
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