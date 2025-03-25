using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Scene.Graphics;

public class SceneFrame(Frame frame,Mat4 view,Mat4 projection,IDeviceBufferView sceneInfo)
{
    public VkCommandBuffer GetCommandBuffer() => frame.GetCommandBuffer();
    public Mat4 View => view;
    public Mat4 Projection => projection;
    
    public Mat4 ViewProjection { get; } = projection * view;

    public Frame Frame => frame;

    public IDeviceBufferView SceneInfo => sceneInfo;
}