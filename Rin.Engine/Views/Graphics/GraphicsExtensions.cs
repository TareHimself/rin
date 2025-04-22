using System.Numerics;
using Rin.Engine.Graphics;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Views.Graphics;

public static class GraphicsExtensions
{
    public static void ConfigureForViews(this Frame frame, in Extent2D extent)
    {
        var cmd = frame.GetCommandBuffer();
        cmd
            .SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
            .SetVertexInput([], [])
            .DisableCulling()
            .DisableDepthTest()
            .EnableBlendingAlphaBlend(0, 1)
            .SetViewports([
                new VkViewport
                {
                    x = 0,
                    y = 0,
                    width = extent.Width,
                    height = extent.Height,
                    minDepth = 0.0f,
                    maxDepth = 1.0f
                }
            ])
            .SetScissors([
                new VkRect2D
                {
                    offset = new VkOffset2D(),
                    extent = new VkExtent2D
                    {
                        width = extent.Width,
                        height = extent.Height
                    }
                }
            ]);
    }
}