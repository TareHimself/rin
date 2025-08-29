using System.Numerics;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Layouts;

namespace Rin.Framework.Views.Composite;

/// <summary>
///     A simple container that draws a rect behind it and contains one child
/// </summary>
public class Rect : SingleSlotCompositeView
{
    public Vector4 BorderRadius;
    public Color Color = Color.Black;

    public override Vector2 ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot) return slot.Child.GetDesiredSize();

        return new Vector2();
    }

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        if (GetSlot() is { } slot)
        {
            slot.Child.Offset = default;
            return slot.Child.ComputeSize(availableSpace);
        }

        return availableSpace;
    }


    protected virtual void CollectSelf(Matrix4x4 transform, CommandList cmds)
    {
        if (Color.A > 0.0f) cmds.AddRect(transform, Size, Color, BorderRadius);
    }

    public override IEnumerable<ISlot> GetSlots()
    {
        if (GetSlot() is { } slot) return [slot];

        return [];
    }

    public override void Collect(in Matrix4x4 transform, in Framework.Graphics.Rect2D clip, CommandList commands)
    {
        if (IsVisible) CollectSelf(transform, commands);
        base.Collect(transform, clip, commands);
    }
}