using rin.Core.Math;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Quads;

namespace rin.Widgets.Containers;

/// <summary>
/// A simple container that draws a rect behind it and contains one child
/// </summary>
public class Rect : ContainerWidget
{
    public Color BackgroundColor = Color.Black;
    public Vector4<float> BorderRadius = 0.0f;
    protected override Vector2<float> ComputeDesiredContentSize()
    {
        if (GetSlot(0) is { } slot)
        {
            return slot.Child.GetDesiredSize();
        }

        return new Vector2<float>();
    }

    protected override Vector2<float> ArrangeContent(Vector2<float> availableSpace)
    {
        if (GetSlot(0) is { } slot)
        {
            slot.Child.Offset = (new Vector2<float>(0, 0));
            return slot.Child.ComputeSize(availableSpace);
        }

        return 0.0f;
    }

    protected virtual void CollectSelf(Matrix3 transform, DrawCommands drawCommands)
    {
        drawCommands.AddRect(transform,Size, color: BackgroundColor, borderRadius: BorderRadius);
    }

    public override void Collect(Matrix3 transform, Widgets.Rect clip, DrawCommands drawCommands)
    {
        if (IsVisible)
        {
            CollectSelf(transform,drawCommands);
        }
        base.Collect(transform,clip, drawCommands);
    }

    public override int GetMaxSlotsCount() => 1;
}