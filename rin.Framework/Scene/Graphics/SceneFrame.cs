using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Scene.Graphics;

public class SceneFrame(Frame frame,Mat4 view,Mat4 projection)
{
    public VkCommandBuffer GetCommandBuffer() => frame.GetCommandBuffer();
    public Mat4 View => view;
    public Mat4 Projection => projection;

    public Frame Frame => frame;
}