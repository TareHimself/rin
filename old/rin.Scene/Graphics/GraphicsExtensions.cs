using rin.Runtime.Graphics;
using rin.Runtime.Graphics.Material;
using rin.Runtime.Core.Math;
using TerraFX.Interop.Vulkan;

namespace rin.Scene.Graphics;

public static class GraphicsExtensions
{
    public static MaterialBuilder ConfigureForScene(this MaterialBuilder builder, bool isTranslucent = false)
    {
        builder.Pipeline.SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL);

        builder.Pipeline
            .SetCullMode(VkCullModeFlags.VK_CULL_MODE_NONE, VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
            .DisableMultisampling()
            .DisableBlending()
            .EnableDepthTest(true, VkCompareOp.VK_COMPARE_OP_LESS_OR_EQUAL,
                VkFormat.VK_FORMAT_D32_SFLOAT);

        if (isTranslucent)
            builder.Pipeline
                .EnableBlendingAdditive()
                .EnableDepthTest(false, VkCompareOp.VK_COMPARE_OP_LESS_OR_EQUAL,
                    VkFormat.VK_FORMAT_D32_SFLOAT);


        builder.AddAttachmentFormats(
            ImageFormat.Rgba16,
            ImageFormat.Rgba32,
            ImageFormat.Rgba32,
            ImageFormat.Rgba16,
            ImageFormat.Rgba16
            );

        return builder;
    }

    public static void ConfigureForScene(this Frame frame,Vector2<uint> extent)
    {
        var cmd = frame.GetCommandBuffer();
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
                    width = extent.X,
                    height = extent.Y,
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
                        width = extent.X,
                        height = extent.Y
                    }
                }
            ]);
    }
}