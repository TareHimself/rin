using rin.Core.Math;

namespace rin.Widgets.Containers;

public enum FitMode
{
    Fill,
    Contain,
    Cover,
    None
}

/// <summary>
/// Slot = <see cref="ContainerSlot"/>
/// </summary>
public class FitterContainer : Container
{

    private FitMode _fitFittingMode = FitMode.Fill;
    
    public FitterContainer() : base()
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
                TryUpdateDesiredSize();
                SizeContent(GetContentSize());
            }
        }
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        if (GetSlot(0) is { } slot)
        {
            return slot.Child.GetDesiredSize();
        }
        return 0.0f;
    }

    public override int GetMaxSlots() => 1;

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

    public void SizeContent(Vector2<float> drawSize)
    {
        if (GetSlot(0) is { } slot)
        {
            var widget = slot.Child;
            var widgetSize = widget.GetDesiredSize();
            var newDrawSize = widgetSize.Equals(drawSize)
                ? widgetSize
                : _fitFittingMode switch
                {
                    FitMode.Fill => drawSize,
                    FitMode.Contain => ComputeContainSize(drawSize, widgetSize),
                    FitMode.Cover => ComputeCoverSize(drawSize, widgetSize),
                    FitMode.None => widgetSize,
                    _ => throw new ArgumentOutOfRangeException()
                };

            if (!newDrawSize.Equals(widget.GetContentSize())) widget.Size = newDrawSize;


            var halfSelfDrawSize = drawSize;
            halfSelfDrawSize /= 2.0f;
            var halfSlotDrawSize = newDrawSize;
            halfSlotDrawSize /= 2.0f;

            var diff = halfSelfDrawSize - halfSlotDrawSize;

            if (!widget.Offset.Equals(diff)) widget.Offset = diff;
        }
    }

    protected override void ArrangeSlots(Vector2<float> drawSize)
    {
        SizeContent(drawSize);
    }
    
}