using System.Numerics;
using rin.Framework.Graphics;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Views.Graphics;

public static class GraphicsExtensions
{
    public static void ConfigureForViews(this Frame frame, Vector2 size)
    {
        var cmd = frame.GetCommandBuffer();
        var surfaceSize = size;
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
                    width = surfaceSize.X,
                    height = surfaceSize.Y,
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
                        width = (uint)surfaceSize.X,
                        height = (uint)surfaceSize.Y
                    }
                }
            ]);
    }
}