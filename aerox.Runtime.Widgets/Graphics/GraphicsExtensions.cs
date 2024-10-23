﻿using aerox.Runtime.Graphics;
using aerox.Runtime.Math;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Widgets.Graphics;
using static TerraFX.Interop.Vulkan.Vulkan;
public static class GraphicsExtensions
{
    public static void ConfigureForWidgets(this Frame frame,Vector2<uint> size)
    {

        var cmd = frame.GetCommandBuffer();
        var surfaceSize = size;
        cmd
            .SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
            .DisableCulling()
            .DisableDepthTest()
            .EnableBlendingAlphaBlend(0, 1)
            .SetViewports([
                new VkViewport()
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
                new VkRect2D()
                {
                    offset = new VkOffset2D(),
                    extent = new VkExtent2D()
                    {
                        width = surfaceSize.X,
                        height = surfaceSize.Y
                    }
                }
            ]);
        const VkStencilFaceFlags faceMask = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;
        vkCmdSetStencilTestEnable(cmd,1);
        vkCmdSetStencilReference(cmd,faceMask,255);
        vkCmdSetStencilWriteMask(cmd,faceMask,0x01);
        vkCmdSetStencilOp(cmd,faceMask,VkStencilOp.VK_STENCIL_OP_KEEP,VkStencilOp.VK_STENCIL_OP_KEEP,VkStencilOp.VK_STENCIL_OP_KEEP,VkCompareOp.VK_COMPARE_OP_NEVER);
        
    }
}