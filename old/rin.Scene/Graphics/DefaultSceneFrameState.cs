using rin.Runtime.Graphics;
using TerraFX.Interop.Vulkan;

namespace rin.Scene.Graphics;

public class DefaultSceneFrameState(SceneDrawer drawer) : FrameState
{
    public override void Apply(Frame frame)
    {
        var cmd = frame.GetCommandBuffer();
        var drawSize = drawer.Size;
        cmd
            .SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
            .DisableStencilTest(false)
            .DisableCulling()
            .EnableDepthTest(true,VkCompareOp.VK_COMPARE_OP_LESS_OR_EQUAL)
            .EnableBlendingAdditive(0, 5)
            .SetViewports([
                new VkViewport()
                {
                    x = 0,
                    y = 0,
                    width = drawSize.X,
                    height = drawSize.Y,
                    minDepth = 0.0f,
                    maxDepth = 1.0f
                }
            ])
            .SetScissors([
                new VkRect2D()
                {
                    offset = new VkOffset2D(),
                    extent = new VkExtent2D()
                    {
                        width = (uint)drawSize.X,
                        height = (uint)drawSize.Y
                    }
                }
            ]);
    }
}