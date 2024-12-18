using System.Diagnostics;
using rin.Framework.Graphics;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Views.Graphics.Commands;

/// <summary>
/// Copies the draw image into the copy image to allow for effects like blur
/// </summary>
public class ReadBack : CustomCommand
{
    public override void Run(WidgetFrame frame, uint stencilMask,IDeviceBuffer? view = null)
    {
        if(frame.IsAnyPassActive) frame.EndActivePass();
        var cmd = frame.Raw.GetCommandBuffer();
        var drawImage = frame.DrawImage;
        var copyImage = frame.CopyImage;
        drawImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
            VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL);
        copyImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
            VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL);
        drawImage.CopyTo(cmd,copyImage);
        drawImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);
        copyImage.Barrier(cmd, 
            VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
    }

    public override bool CombineWith(CustomCommand other)
    {
        return other is ReadBack;
    }

    public override bool WillDraw => false;
    public override CommandStage Stage => CommandStage.Maintain;
}