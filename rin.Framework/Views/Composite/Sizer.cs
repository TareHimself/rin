﻿using rin.Framework.Core.Math;
using rin.Framework.Core.Animation;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Animation;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Quads;

namespace rin.Framework.Views.Composite;

/// <summary>
/// Slot = <see cref="CompositeViewSlot"/>
/// </summary>
public class Sizer : CompositeView
{
    private float? _heightOverride;
    private float? _widthOverride;

    public float? WidthOverride
    {
        get => _widthOverride;
        set
        {
            _widthOverride = value;
            Invalidate(InvalidationType.DesiredSize);
        }
    }

    public float? HeightOverride
    {
        get => _heightOverride;
        set
        {
            _heightOverride = value;
            Invalidate(InvalidationType.DesiredSize);
        }
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        if (GetSlot(0) is { } slot)
        {
            var desiredSize = slot.Child.GetDesiredSize();
            return new Vector2<float>(WidthOverride ?? desiredSize.X, HeightOverride ?? desiredSize.Y);
        }

        return new Vector2<float>();
    }

    public override int GetMaxSlotsCount() => 1;

    protected override Vector2<float> ArrangeContent(Vector2<float> availableSpace)
    {
        var size = new Vector2<float>(WidthOverride.GetValueOrDefault(availableSpace.X),
            HeightOverride.GetValueOrDefault(availableSpace.Y));
        if (GetSlot(0) is { } slot)
        {
            slot.Child.Offset = (new Vector2<float>(0, 0)); 
            return slot.Child.ComputeSize(size);
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