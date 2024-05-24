
using aerox.Runtime.Graphics.Material;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Widgets;

public static class MaterialBuilderExtensions
{
    public static MaterialBuilder ConfigureForWidgets(this MaterialBuilder builder)
    {
        builder.Pipeline.SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
            .DisableMultisampling().DisableDepthTest().EnableBlendingAlphaBlend();
        
        return builder;
    }
}