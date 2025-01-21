using System.Runtime.InteropServices;
using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Commands;
using rin.Framework.Core;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Descriptors;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Graphics.Quads;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;





// internal class BlurCommand : Command
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
//     // protected override void Draw(ViewFrame frame)
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
//     protected override void Draw(ViewFrame frame)
//     {
//         throw new NotImplementedException();
//     }
// }


public class BackgroundBlur : SingleSlotCompositeView
{
    public Color Tint = Color.White;
    public float Strength { get; set; } = 7.0f;
    protected override Vec2<float> ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot)
        {
            return slot.Child.GetDesiredSize();
        }
        return 0.0f;
    }
    
    // public override void Collect(ViewFrame frame, TransformInfo info)
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
    //         var slotDrawInfo = info.AccountFor(slot.GetView());
    //         if (slotDrawInfo.Occluded) continue;
    //         slot.GetView().Collect(frame, info.AccountFor(slot.GetView()));
    //     }
    // }

    public override void Collect(Mat3 transform, Views.Rect clip, DrawCommands drawCommands)
    {
        if (IsVisible)
        {
            drawCommands.Add(new ReadBack());
            drawCommands.Add(new BlurCommand(transform,Size,Strength,Tint));
        }
        base.Collect(transform,clip, drawCommands);
    }

    protected override Vec2<float> ArrangeContent(Vec2<float> availableSpace)
    {
        if (GetSlot() is { } slot)
        {
            slot.Child.Offset = (new Vec2<float>(0, 0));
            return slot.Child.ComputeSize(availableSpace);
        }

        return availableSpace;
    }
}