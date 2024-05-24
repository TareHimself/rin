using aerox.Runtime.Graphics.Material;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Scene;

public static class MaterialBuilderExtensions
{
    public static MaterialBuilder ConfigureForScene(this MaterialBuilder builder,bool isTranslucent = false)
    {
        builder.Pipeline.SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL);
        
        builder.Pipeline
            .SetCullMode(VkCullModeFlags.VK_CULL_MODE_NONE, VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
            .DisableBlending()
            .EnableDepthTest(true, VkCompareOp.VK_COMPARE_OP_LESS_OR_EQUAL,
                VkFormat.VK_FORMAT_D32_SFLOAT);

        if (isTranslucent)
        {
            builder.Pipeline
            .EnableBlendingAdditive()
            .EnableDepthTest(false, VkCompareOp.VK_COMPARE_OP_LESS_OR_EQUAL,
                VkFormat.VK_FORMAT_D32_SFLOAT);
        }
        
        return builder;
    }
}