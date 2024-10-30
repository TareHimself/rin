using System.Runtime.InteropServices;
using rin.Graphics;
using rin.Graphics.Descriptors;
using rin.Core.Math;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Commands;

namespace rin.Widgets.Containers;

[StructLayout(LayoutKind.Sequential)]
public struct BlurPushConstants
{
    public Matrix3 Transform;

    public Vector2<float> Size;

    public float BlurRadius;

    public Vector4<float> Tint;
}

// internal class BlurCommand : DrawCommand
// {
//     // private readonly MaterialInstance _materialInstance;
//     // private readonly BlurPushConstants _pushConstants;
//     //
//     // public BlurCommand(MaterialInstance materialInstance, BlurPushConstants pushConstants)
//     // {
//     //     materialInstance.Reserve();
//     //     _materialInstance = materialInstance;
//     //     _pushConstants = pushConstants;
//     // }
//     //
//     // protected override void OnDispose(bool isManual)
//     // {
//     //     base.OnDispose(isManual);
//     //     _materialInstance.Dispose();
//     // }
//     //
//     // protected override void Draw(WidgetFrame frame)
//     // {
//     //     
//     //     _materialInstance.BindImage("SourceT", frame.Surface.GetCopyImage(), DescriptorSet.ImageType.Texture, new SamplerSpec
//     //     {
//     //         Filter = ImageFilter.Linear,
//     //         Tiling = ImageTiling.Repeat
//     //     });
//     //     _materialInstance.BindTo(frame);
//     //     _materialInstance.BindBuffer("ui", frame.Surface.GlobalBuffer);
//     //     _materialInstance.Push(frame.Raw.GetCommandBuffer(),  _pushConstants);
//     //     Quad(frame);
//     // }
//     protected override void Draw(WidgetFrame frame)
//     {
//         throw new NotImplementedException();
//     }
// }

public class BackgroundBlur : Container
{
    public Color Tint = Color.White;

    public BackgroundBlur(Widget child) : base([child])
    {
    }

    public BackgroundBlur()
    {
    }


    protected override Size2d ComputeDesiredContentSize()
    {
        if (GetSlot(0) is { } slot)
        {
            return slot.GetWidget().GetDesiredSize();
        }
        return new Size2d();
    }
    
    // public override void Collect(WidgetFrame frame, TransformInfo info)
    // {
    //     frame.AddCommands(new ReadBack(),new BlurCommand(_materialInstance, new BlurPushConstants
    //     {
    //         Transform = info.Transform,
    //         Size = GetContentSize(),
    //         BlurRadius = 20.0f,
    //         Tint = Tint
    //     }));
    //
    //     foreach (var slot in _slot.ToArray())
    //     {
    //         var slotDrawInfo = info.AccountFor(slot.GetWidget());
    //         if (slotDrawInfo.Occluded) continue;
    //         slot.GetWidget().Collect(frame, info.AccountFor(slot.GetWidget()));
    //     }
    // }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        if (GetSlot(0) is { } slot)
        {
            var widget = slot.GetWidget();
            widget.SetOffset(0.0f);
            widget.SetSize(drawSize);
        }
    }

    public override int GetMaxSlots() => 1;
}