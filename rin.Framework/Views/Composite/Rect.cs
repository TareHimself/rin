using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Quads;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;

/// <summary>
///     A simple container that draws a rect behind it and contains one child
/// </summary>
public class Rect : SingleSlotCompositeView
{
    public Color BackgroundColor = Color.Black;
    public Vector4 BorderRadius;

    protected override Vector2 ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot) return slot.Child.GetDesiredSize();

        return new Vector2();
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


    protected virtual void CollectSelf(Mat3 transform, PassCommands passCommands)
    {
        if (BackgroundColor.A > 0.0f) passCommands.AddRect(transform, Size, BackgroundColor, BorderRadius);
    }

    public override IEnumerable<ISlot> GetSlots()
    {
        if (GetSlot() is { } slot) return [slot];

        return [];
    }

    public override void Collect(Mat3 transform, Views.Rect clip, PassCommands passCommands)
    {
        if (IsVisible) CollectSelf(transform, passCommands);
        base.Collect(transform, clip, passCommands);
    }
}