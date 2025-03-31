using System.Numerics;
using Rin.Engine.Core.Math;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Layouts;
using Rin.Engine.Views.Graphics.Quads;

namespace Rin.Engine.Views.Composite;

/// <summary>
///     A simple container that draws a rect behind it and contains one child
/// </summary>
public class Rect : SingleSlotCompositeView
{
    public Color Color = Color.Black;
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


    protected virtual void CollectSelf(Matrix4x4 transform, PassCommands passCommands)
    {
        if (Color.A > 0.0f) passCommands.AddRect(transform, Size, Color, BorderRadius);
    }

    public override IEnumerable<ISlot> GetSlots()
    {
        if (GetSlot() is { } slot) return [slot];

        return [];
    }

    public override void Collect(Matrix4x4 transform, Views.Rect clip, PassCommands passCommands)
    {
        if (IsVisible) CollectSelf(transform, passCommands);
        base.Collect(transform, clip, passCommands);
    }
}