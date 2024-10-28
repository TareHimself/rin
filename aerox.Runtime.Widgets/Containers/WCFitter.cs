using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Containers;

public enum FitMode
{
    Fill,
    Contain,
    Cover,
    None
}

public class WCFitter : Container
{
    private FitMode _fitFittingMode = FitMode.Fill;
    
    public WCFitter(Widget? widget = null) : base(widget == null ? [] : [widget])
    {
        Clip = ClipMode.Bounds;
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

    protected override Size2d ComputeDesiredContentSize()
    {
        if (GetSlot(0) is { } slot)
        {
            return slot.GetWidget().GetDesiredSize();
        }
        return new Size2d();
    }

    public override int GetMaxSlots() => 1;

    public static Size2d ComputeContainSize(Size2d drawSize, Size2d widgetSize)
    {
        var widgetAspect = widgetSize.Height / widgetSize.Width;
        var scaledWidgetSize = new Size2d(drawSize.Width, drawSize.Width * widgetAspect);

        if (drawSize.Equals(scaledWidgetSize)) return scaledWidgetSize;
        
        return scaledWidgetSize.Height <= drawSize.Height
            ? scaledWidgetSize
            : new Size2d(drawSize.Height / widgetAspect, drawSize.Height);
    }

    public static Size2d ComputeCoverSize(Size2d drawSize, Size2d widgetSize)
    {
        var widgetAspect = widgetSize.Height / widgetSize.Width;
        var scaledWidgetSize = new Size2d(drawSize.Width, drawSize.Width * widgetAspect);

        if (drawSize.Equals(scaledWidgetSize)) return scaledWidgetSize;


        return scaledWidgetSize.Height <= drawSize.Height
            ? new Size2d(drawSize.Height / widgetAspect, drawSize.Height)
            : scaledWidgetSize;
    }

    public void SizeContent(Size2d drawSize)
    {
        if (GetSlot(0) is { } slot)
        {
            var widget = slot.GetWidget();
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

            if (!newDrawSize.Equals(widget.GetContentSize())) widget.SetSize(newDrawSize);


            Vector2<float> halfSelfDrawSize = drawSize;
            halfSelfDrawSize /= 2.0f;
            Vector2<float> halfSlotDrawSize = newDrawSize;
            halfSlotDrawSize /= 2.0f;

            var diff = halfSelfDrawSize - halfSlotDrawSize;

            if (!widget.GetOffset().Equals(diff)) widget.SetOffset(diff);
        }
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        SizeContent(drawSize);
    }
    
}