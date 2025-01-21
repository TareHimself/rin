using rin.Framework.Core.Math;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;

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
    /// Adds the View to this container
    /// </summary>
    
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
    

    protected override Vec2<float> ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot)
        {
            return slot.Child.GetDesiredSize();
        }

        return 0.0f;
    }

    public static Vec2<float> ComputeContainSize(Vec2<float> drawSize, Vec2<float> viewSize)
    {
        var viewAspect = viewSize.Y / viewSize.X;
        var scaledViewSize = new Vec2<float>(drawSize.X, drawSize.X * viewAspect);

        if (drawSize.Equals(scaledViewSize)) return scaledViewSize;

        return scaledViewSize.Y <= drawSize.Y
            ? scaledViewSize
            : new Vec2<float>(drawSize.Y / viewAspect, drawSize.Y);
    }

    public static Vec2<float> ComputeCoverSize(Vec2<float> drawSize, Vec2<float> viewSize)
    {
        var viewAspect = viewSize.Y / viewSize.X;
        var scaledViewSize = new Vec2<float>(drawSize.X, drawSize.X * viewAspect);

        if (drawSize.Equals(scaledViewSize)) return scaledViewSize;


        return scaledViewSize.Y <= drawSize.Y
            ? new Vec2<float>(drawSize.Y / viewAspect, drawSize.Y)
            : scaledViewSize;
    }

    public Vec2<float> FitContent(Vec2<float> drawSize)
    {
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

            return drawSize;
        }

        return drawSize;
    }

    protected override Vec2<float> ArrangeContent(Vec2<float> availableSpace)
    {
        var desired = GetDesiredContentSize();
        return FitContent(new Vec2<float>(float.IsFinite(availableSpace.X) ? availableSpace.X : desired.X,
            float.IsFinite(availableSpace.Y) ? availableSpace.Y : desired.Y));
    }

    public override void OnChildInvalidated(View child, InvalidationType invalidation)
    {
        FitContent(GetContentSize());
    }

    public override IEnumerable<ISlot> GetSlots()
    {
        if (GetSlot() is { } slot)
        {
            return [slot];
        }

        return [];
    }
}