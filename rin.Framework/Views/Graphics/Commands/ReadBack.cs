using System.Diagnostics;
using rin.Framework.Graphics;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Views.Graphics.Commands;

/// <summary>
/// Copies the draw image into the copy image to allow for effects like blur
/// </summary>
public class ReadBack : CustomCommand
{
    public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBufferView? view = null)
    {
        if(frame.IsAnyPassActive) frame.EndActivePass();
        var cmd = frame.Raw.GetCommandBuffer();
        var drawImage = frame.DrawImage;
        var copyImage = frame.CopyImage;
        cmd.ImageBarrier(drawImage, ImageLayout.ColorAttachment,
            ImageLayout.TransferSrc);
        cmd.ImageBarrier(copyImage, ImageLayout.ShaderReadOnly,
            ImageLayout.TransferDst);
        cmd.CopyImageToImage(drawImage, copyImage);
        cmd.ImageBarrier(drawImage, ImageLayout.TransferSrc,ImageLayout.ColorAttachment);
        cmd.ImageBarrier(copyImage, 
            ImageLayout.TransferDst,ImageLayout.ShaderReadOnly);
    }

    public override bool CombineWith(CustomCommand other)
    {
        return other is ReadBack;
    }

    public override ulong GetRequiredMemory() => 0;

    public override bool WillDraw() => false;
}