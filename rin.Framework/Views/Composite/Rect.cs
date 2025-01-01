using rin.Framework.Core.Math;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Quads;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;

/// <summary>
/// A simple container that draws a rect behind it and contains one child
/// </summary>
public class Rect : SingleSlotCompositeView
{
    
    public Color BackgroundColor = Color.Black;
    public Vec4<float> BorderRadius = 0.0f;

    protected override Vec2<float> ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot)
        {
            return slot.Child.GetDesiredSize();
        }

        return new Vec2<float>();
    }

    protected override Vec2<float> ArrangeContent(Vec2<float> availableSpace)
    {
        if (GetSlot() is { } slot)
        {
            slot.Child.Offset = (new Vec2<float>(0, 0));
            return slot.Child.ComputeSize(availableSpace);
        }

        return 0.0f;
    }
    

    protected virtual void CollectSelf(Mat3 transform, DrawCommands drawCommands)
    {
        if (BackgroundColor.A > 0.0f)
        {
            drawCommands.AddRect(transform,Size, color: BackgroundColor, borderRadius: BorderRadius);
        }
    }

    public override IEnumerable<ISlot> GetSlots()
    {
        if (GetSlot() is { } slot)
        {
            return [slot];
        }

        return [];
    }

    public override void Collect(Mat3 transform, Views.Rect clip, DrawCommands drawCommands)
    {
        if (IsVisible)
        {
            CollectSelf(transform,drawCommands);
        }
        base.Collect(transform,clip, drawCommands);
    }
}