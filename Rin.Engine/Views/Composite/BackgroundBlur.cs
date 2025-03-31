using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Core.Math;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Composite;

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
    [PublicAPI] public Color Tint = Color.White;

    [PublicAPI] public float Strength { get; set; } = 5.0f;

    [PublicAPI] public float Radius { get; set; } = 3.0f;

    protected override Vector2 ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot) return slot.Child.GetDesiredSize();
        return new Vector2();
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

    public override void Collect(Matrix4x4 transform, Views.Rect clip, PassCommands passCommands)
    {
        if (IsVisible && Strength > 0.0f && Radius > 0.0f)
        {
            passCommands.Add(new ReadBack());
            passCommands.Add(new BlurCommand(transform, Size, Strength, Radius, Tint));
        }

        base.Collect(transform, clip, passCommands);
    }

    protected override Vector2 ArrangeContent(Vector2 availableSpace)
    {
        if (GetSlot() is { } slot)
        {
            slot.Child.Offset = default;
            return slot.Child.ComputeSize(availableSpace);
        }

        return availableSpace;
    }
}