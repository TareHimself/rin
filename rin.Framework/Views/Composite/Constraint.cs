using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Core.Animation;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Animation;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Quads;

namespace rin.Framework.Views.Composite;

/// <summary>
/// </summary>
public class Constraint : SingleSlotCompositeView
{
    private float? _minWidth;
    private float? _maxWidth;
    private float? _minHeight;
    private float? _maxHeight;

    public float? MinWidth
    {
        get => _minWidth;
        set
        {
            _minWidth = value;
            Invalidate(InvalidationType.DesiredSize);
        }
    }

    public float? MaxWidth
    {
        get => _maxWidth;
        set
        {
            _maxWidth = value;
            Invalidate(InvalidationType.DesiredSize);
        }
    }
    
    public float? MinHeight
    {
        get => _minHeight;
        set
        {
            _minHeight = value;
            Invalidate(InvalidationType.DesiredSize);
        }
    }

    public float? MaxHeight
    {
        get => _maxHeight;
        set
        {
            _maxHeight = value;
            Invalidate(InvalidationType.DesiredSize);
        }
    }

    private Vector2 Constrain(Vector2 size)
    {
        var min = new Vector2(MinWidth ?? size.X, MinHeight ?? size.Y);
        var maxX = MaxWidth ?? (size.X >= min.X ? size.X : min.X);
        var maxY = MaxHeight ?? (size.Y >= min.Y ? size.Y : min.Y);
        var max = new Vector2(maxX, maxY);
        return size.Clamp(min,max);
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot)
        {
            var desiredSize = slot.Child.GetDesiredSize();
            return Constrain(desiredSize);
        }

        return new Vector2();
    }

    protected override Vector2 ArrangeContent(Vector2 availableSpace)
    {
       var size = Constrain(availableSpace);
        if (GetSlot() is { } slot)
        {
            slot.Child.Offset = default; 
            return Constrain(slot.Child.ComputeSize(size));
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