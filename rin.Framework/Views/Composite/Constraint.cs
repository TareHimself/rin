﻿using rin.Framework.Core.Math;
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

    private Vec2<float> Constrain(Vec2<float> size)
    {
        var min = new Vec2<float>(MinWidth ?? size.X, MinHeight ?? size.Y);
        var maxX = MaxWidth ?? (size.X >= min.X ? size.X : min.X);
        var maxY = MaxHeight ?? (size.Y >= min.Y ? size.Y : min.Y);
        var max = new Vec2<float>(maxX, maxY);
        return size.Clamp(min,max);
    }

    protected override Vec2<float> ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot)
        {
            var desiredSize = slot.Child.GetDesiredSize();
            return Constrain(desiredSize);
        }

        return new Vec2<float>();
    }

    protected override Vec2<float> ArrangeContent(Vec2<float> availableSpace)
    {
       var size = Constrain(availableSpace);
        if (GetSlot() is { } slot)
        {
            slot.Child.Offset = (new Vec2<float>(0, 0)); 
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