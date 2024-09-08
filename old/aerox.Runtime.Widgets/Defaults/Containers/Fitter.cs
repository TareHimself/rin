using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Defaults.Containers;

public enum FitMode
{
    Fill,
    Contain,
    Cover,
    None
}

public class Fitter : Container
{
    private FitMode _fitFittingMode = FitMode.Fill;

    public Fitter() : base()
    {
        
    }
    public Fitter(Widget widget) : base(widget)
    {
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
                CheckSize();
                SizeContent(GetDrawSize());
            }
        }
    }

    protected override Size2d ComputeDesiredSize()
    {
        return Slots.FirstOrDefault()?.GetWidget().GetDesiredSize() ?? new Size2d();
    }

    public override void Collect(WidgetFrame frame, DrawInfo info)
    {
        foreach (var slot in Slots)
        {
            var widget = slot.GetWidget();
            widget.Collect(frame, info.AccountFor(widget));
        }
    }

    public override uint GetMaxSlots()
    {
        return 1;
    }

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
        var slot = Slots.FirstOrDefault();
        if (slot == null) return;
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

        if (!newDrawSize.Equals(widget.GetDrawSize())) widget.SetDrawSize(newDrawSize);


        Vector2<float> halfSelfDrawSize = drawSize;
        halfSelfDrawSize /= 2.0f;
        Vector2<float> halfSlotDrawSize = newDrawSize;
        halfSlotDrawSize /= 2.0f;

        var diff = halfSelfDrawSize - halfSlotDrawSize;

        if (!widget.GetOffset().Equals(diff)) widget.SetRelativeOffset(diff);
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        SizeContent(drawSize);
    }

    public override void OnChildResized(Widget widget)
    {
        CheckSize();
        ArrangeSlots(GetDrawSize());
    }
}