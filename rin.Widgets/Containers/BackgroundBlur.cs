using System.Runtime.InteropServices;
using rin.Core;
using rin.Graphics;
using rin.Graphics.Descriptors;
using rin.Core.Math;
using rin.Graphics.Shaders;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Commands;

namespace rin.Widgets.Containers;





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

/// <summary>
/// Slot = <see cref="ContainerSlot"/>
/// </summary>
public class BackgroundBlur : Container
{
    public Color Tint = Color.White;
    public float Strength { get; set; } = 7.0f;
    protected override Vector2<float> ComputeDesiredContentSize()
    {
        if (GetSlot(0) is { } slot)
        {
            return slot.Child.GetDesiredSize();
        }
        return 0.0f;
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

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        base.CollectContent(info, drawCommands);
        drawCommands.Add(new ReadBack());
        drawCommands.Add(new BlurCommand(info.Transform,GetContentSize(),Strength,Tint));
    }

    protected override Vector2<float> ArrangeContent(Vector2<float> availableSpace)
    {
        if (GetSlot(0) is { } slot)
        {
            var widget = slot.Child;
            widget.Offset = 0.0f;
            return widget.ComputeSize(availableSpace);
        }

        return 0.0f;
    }

    public override int GetMaxSlotsCount() => 1;
}