using Rin.Engine.Graphics;

namespace Rin.Engine.Views.Graphics.Commands;

/// <summary>
///     Copies the draw image into the copy image to allow for effects like blur
/// </summary>
public class ReadBack : CustomCommand
{
    public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBufferView? view = null)
    {
        if (frame.IsAnyPassActive) frame.EndActivePass();
        var cmd = frame.Raw.GetCommandBuffer();
        var drawImage = frame.DrawImage;
        var copyImage = frame.CopyImage;
        cmd.ImageBarrier(drawImage, ImageLayout.TransferSrc)
            .ImageBarrier(copyImage, ImageLayout.TransferDst)
            .CopyImageToImage(drawImage, copyImage)
            .ImageBarrier(drawImage, ImageLayout.ColorAttachment)
            .ImageBarrier(copyImage, ImageLayout.ShaderReadOnly);
    }

    public override bool CombineWith(CustomCommand other)
    {
        return other is ReadBack;
    }

    public override ulong GetRequiredMemory()
    {
        return 0;
    }

    public override bool WillDraw()
    {
        return false;
    }
}