using rin.Core.Math;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Quads;

namespace rin.Widgets.Containers;

/// <summary>
/// A simple container that draws a rect behind it and contains one child
/// </summary>
public class Rect : Container
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

    protected override void ArrangeSlots(Vector2<float> drawSize)
    {
        if (GetSlot(0) is { } slot)
        {
            slot.Child.Offset = (new Vector2<float>(0, 0));
            slot.Child.Size = drawSize;
        }
    }

    protected override void CollectSelf(TransformInfo info, DrawCommands drawCommands)
    {
        base.CollectSelf(info, drawCommands);
        drawCommands.AddRect(info.Transform, info.Size, color: BackgroundColor, borderRadius: BorderRadius);
    }

    public override int GetMaxSlotsCount() => 1;
}