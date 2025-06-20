﻿using System.Numerics;
using Rin.Engine.Views.Enums;
using Rin.Engine.Views.Layouts;

namespace Rin.Engine.Views.Composite;

public enum FitMode
{
    Fill,
    Contain,
    Cover,
    None
}

/// <summary>
/// </summary>
public class Fitter : SingleSlotCompositeView
{
    private FitMode _fitFittingMode = FitMode.Fill;

    /// <summary>
    ///     Adds the View to this container
    /// </summary>
    public Fitter()
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
            if (_fitFittingMode != old) FitContent(GetContentSize());
        }
    }


    protected override Vector2 ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot) return slot.Child.GetDesiredSize();

        return new Vector2();
    }

    public static Vector2 ComputeContainSize(Vector2 drawSize, Vector2 desiredSize)
    {
        var desiredAspect = desiredSize.Y / desiredSize.X;
        
        var xConstrainedSize = drawSize with { Y = drawSize.X * desiredAspect };
        
        return xConstrainedSize.Y <= drawSize.Y
            ? xConstrainedSize
            : drawSize with { X = drawSize.Y / desiredAspect };
    }

    public static Vector2 ComputeCoverSize(Vector2 drawSize, Vector2 desiredSize)
    {
        var viewAspect = desiredSize.Y / desiredSize.X;
        var scaledViewSize = new Vector2(drawSize.X, drawSize.X * viewAspect);

        if (drawSize == scaledViewSize) return scaledViewSize;


        return scaledViewSize.Y <= drawSize.Y
            ? new Vector2(drawSize.Y / viewAspect, drawSize.Y)
            : scaledViewSize;
    }

    private Vector2 FitContent(Vector2 drawSize)
    {
        // If we have any content fit it 
        if (GetSlot() is { } slot)
        {
            var view = slot.Child;
            var viewSize = view.GetDesiredSize();
            var newDrawSize = _fitFittingMode switch
            {
                FitMode.Fill => drawSize,
                FitMode.Contain => ComputeContainSize(drawSize, viewSize),
                FitMode.Cover => ComputeCoverSize(drawSize, viewSize),
                FitMode.None => viewSize,
                _ => throw new ArgumentOutOfRangeException()
            };

            view.ComputeSize(newDrawSize);
            
            var halfSelfDrawSize = drawSize;
            halfSelfDrawSize /= 2.0f;
            var halfSlotDrawSize = newDrawSize;
            halfSlotDrawSize /= 2.0f;

            var diff = halfSelfDrawSize - halfSlotDrawSize;

            view.Offset = diff;
        }

        return drawSize;
    }

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        var desired = GetDesiredContentSize();
        return FitContent(new Vector2(float.IsFinite(availableSpace.X) ? availableSpace.X : desired.X,
            float.IsFinite(availableSpace.Y) ? availableSpace.Y : desired.Y));
    }

    public override void OnChildInvalidated(View child, InvalidationType invalidation)
    {
        FitContent(GetContentSize());
    }

    public override IEnumerable<ISlot> GetSlots()
    {
        if (GetSlot() is { } slot) return [slot];

        return [];
    }
}