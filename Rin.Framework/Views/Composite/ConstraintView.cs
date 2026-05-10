using System.Numerics;
using Rin.Framework.Shared.Math;

namespace Rin.Framework.Views.Composite;

/// <summary>
/// </summary>
public class ConstraintView : SingleSlotCompositeView
{
    public float? MinWidth
    {
        get;
        set
        {
            field = value;
            InvalidateDesiredSize();
            InvalidateLayout();
        }
    }

    public float? MaxWidth
    {
        get;
        set
        {
            field = value;
            InvalidateDesiredSize();
            InvalidateLayout();
        }
    }

    public float? MinHeight
    {
        get;
        set
        {
            field = value;
            InvalidateDesiredSize();
            InvalidateLayout();
        }
    }

    public float? MaxHeight
    {
        get;
        set
        {
            field = value;
            InvalidateDesiredSize();
            InvalidateLayout();
        }
    }

    private Vector2 Constrain(Vector2 size)
    {
        var min = new Vector2(MinWidth ?? size.X, MinHeight ?? size.Y);
        var maxX = MaxWidth ?? (size.X >= min.X ? size.X : min.X);
        var maxY = MaxHeight ?? (size.Y >= min.Y ? size.Y : min.Y);
        var max = new Vector2(maxX, maxY);
        return size.Clamp(min, max);
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot)
        {
            var desiredSize = slot.Child.GetDesiredSize();
            return Constrain(desiredSize);
        }

        return new Vector2();
    }

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        var size = Constrain(availableSpace);
        if (GetSlot() is { } slot)
        {
            slot.Child.Offset = default;
            return Constrain(slot.Child.Layout(size));
        }

        return size;
    }


    // protected override void CollectSelf(TransformInfo info, DrawCommands drawCommands)
    // {
    //     base.CollectSelf(info, drawCommands);
    //     drawCommands.AddRect(info.Transform, info.Size, color: Color.Green);
    //     drawCommands.AddRect(info.Transform *Matrix3.Identity.Translate(1.5f), info.Size - 3f, color: Color.Red);
    //     
    // }
}