﻿using rin.Framework.Core.Math;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Composite;

public enum FitMode
{
    Fill,
    Contain,
    Cover,
    None
}

/// <summary>
/// Slot = <see cref="CompositeViewSlot"/>
/// </summary>
public class Fitter : CompositeView
{

    private FitMode _fitFittingMode = FitMode.Fill;

    public Fitter() : base()
    {
        Clip = Clip.Bounds;
    }

    public FitMode FittingMode
    {
        get => _fitFittingMode;
        set
        {
            var old = _fitFittingMode;
            _fitFittingMode = value;
            if (_fitFittingMode != old)
            {
                FitContent(GetContentSize());
            }
        }
    }

    public override void OnSlotInvalidated(CompositeViewSlot slot, InvalidationType invalidation){

        if (FittingMode != FitMode.None)
        {
            base.OnSlotInvalidated(slot, invalidation);  
        }
        FitContent(GetContentSize());
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        if (GetSlot(0) is { } slot)
        {
            return slot.Child.GetDesiredSize();
        }

        return 0.0f;
    }

    public override int GetMaxSlotsCount() => 1;

    public static Vector2<float> ComputeContainSize(Vector2<float> drawSize, Vector2<float> widgetSize)
    {
        var widgetAspect = widgetSize.Y / widgetSize.X;
        var scaledWidgetSize = new Vector2<float>(drawSize.X, drawSize.X * widgetAspect);

        if (drawSize.Equals(scaledWidgetSize)) return scaledWidgetSize;

        return scaledWidgetSize.Y <= drawSize.Y
            ? scaledWidgetSize
            : new Vector2<float>(drawSize.Y / widgetAspect, drawSize.Y);
    }

    public static Vector2<float> ComputeCoverSize(Vector2<float> drawSize, Vector2<float> widgetSize)
    {
        var widgetAspect = widgetSize.Y / widgetSize.X;
        var scaledWidgetSize = new Vector2<float>(drawSize.X, drawSize.X * widgetAspect);

        if (drawSize.Equals(scaledWidgetSize)) return scaledWidgetSize;


        return scaledWidgetSize.Y <= drawSize.Y
            ? new Vector2<float>(drawSize.Y / widgetAspect, drawSize.Y)
            : scaledWidgetSize;
    }

    public Vector2<float> FitContent(Vector2<float> drawSize)
    {
        if (GetSlot(0) is { } slot)
        {
            var widget = slot.Child;
            var widgetSize = widget.GetDesiredSize();
            var newDrawSize = _fitFittingMode switch
                {
                    FitMode.Fill => drawSize,
                    FitMode.Contain => ComputeContainSize(drawSize, widgetSize),
                    FitMode.Cover => ComputeCoverSize(drawSize, widgetSize),
                    FitMode.None => widgetSize,
                    _ => throw new ArgumentOutOfRangeException()
                };

            widget.ComputeSize(newDrawSize);


            var halfSelfDrawSize = drawSize;
            halfSelfDrawSize /= 2.0f;
            var halfSlotDrawSize = newDrawSize;
            halfSlotDrawSize /= 2.0f;

            var diff = halfSelfDrawSize - halfSlotDrawSize;

            widget.Offset = diff;

            return drawSize;
        }

        return drawSize;
    }

    protected override Vector2<float> ArrangeContent(Vector2<float> availableSpace)
    {
        var desired = GetDesiredContentSize();
        return FitContent(new Vector2<float>(float.IsFinite(availableSpace.X) ? availableSpace.X : desired.X,
            float.IsFinite(availableSpace.Y) ? availableSpace.Y : desired.Y));
    }

    public override void Collect(Matrix3 transform, Views.Rect clip, DrawCommands drawCommands)
    {
        base.Collect(transform,clip, drawCommands);
    }
}