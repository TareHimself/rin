using System.Numerics;
using Rin.Engine.Graphics;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.World.Graphics;

public class SceneFrame(
    Matrix4x4 view,
    Matrix4x4 projection,
    IDeviceBufferView sceneInfo,
    VkCommandBuffer commandBuffer)
{
    public Matrix4x4 View => view;
    public Matrix4x4 Projection => projection;

    public Matrix4x4 ViewProjection { get; } = projection * view;
    
    public IDeviceBufferView SceneInfo => sceneInfo;

    public VkCommandBuffer GetCommandBuffer()
    {
        return commandBuffer;
    }
}