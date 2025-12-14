using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Views.Graphics.Blur;
using Rin.Framework.Views.Graphics.Commands;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Graphics.Vector;

namespace Rin.Framework.Views.Composite;

public class BackgroundBlurView : SingleSlotCompositeView
{
    [PublicAPI] public Color Tint = Color.White;

    [PublicAPI] public float Strength { get; set; } = 5.0f;

    [PublicAPI] public float Radius { get; set; } = 3.0f;

    public override Vector2 ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot) return slot.Child.GetDesiredSize();
        return new Vector2();
    }

    public override void Collect(in Matrix4x4 transform, in Framework.Graphics.Rect2D clip, CommandList commands)
    {
        if (IsVisible && Strength > 0.0f && Radius > 0.0f)
        {
            //commands.AddBlur(transform, GetContentSize(), Strength, Radius, Tint);
            // commandList.Add(new ReadBack());
            commands.AddPath(Matrix4x4.Identity).LineTo(new Vector2(100, 0)).Stroke();
            commands.AddBlur(transform, GetSize(), Strength, Radius, Tint);
        }

        base.Collect(transform, clip, commands);
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
}