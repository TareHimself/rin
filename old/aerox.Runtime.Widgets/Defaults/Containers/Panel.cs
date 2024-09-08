using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Defaults.Containers;

/// <summary>
///     Describes how a <see cref="Widget" /> will be sized and positioned in a <see cref="Panel" />. Supports Anchors
/// </summary>
public class PanelSlot(Panel panel, Widget widget) : Slot(widget)
{
    public Vector2<float> Alignment = new(0, 0);
    public Vector2<float> MaxAnchor = new(0, 0);
    public Vector2<float> MinAnchor = new(0, 0);
    public Vector2<float> Offset = new(0, 0);
    public Size2d Size = new();
    public bool SizeToContent { get; set; } = false;

    private static bool NearlyEqual(double a, double b, double tolerance = 0.001f)
    {
        return System.Math.Abs(a - b) < tolerance;
    }


    public PanelSlot Mutate(Action<PanelSlot> mutation)
    {
        mutation(this);
        ComputeSizeAndOffset();
        return this;
    }

    public void ComputeSizeAndOffset()
    {
        var panelSize = panel.GetDrawSize();

        var noOffsetX = NearlyEqual(MinAnchor.X, MaxAnchor.X);
        var noOffsetY = NearlyEqual(MinAnchor.Y, MaxAnchor.Y);

        var widgetSize = widget.GetDesiredSize();
        var wSize = new Size2d
        {
            Width = SizeToContent && noOffsetX ? widgetSize.Width : Size.Width,
            Height = SizeToContent && noOffsetY ? widgetSize.Height : Size.Height
        };

        var p1 = Offset.Clone();
        var p2 = p1 + wSize;

        if (noOffsetX)
        {
            var a = panelSize.Width * MinAnchor.X;
            p1.X += a;
            p2.X += a;
        }
        else
        {
            p1.X = panelSize.Width * MinAnchor.X + p1.X;
            p2.X = panelSize.Width * MaxAnchor.X - p2.X;
        }

        if (noOffsetY)
        {
            var a = panelSize.Height * MinAnchor.Y;
            p1.Y += a;
            p2.Y += a;
        }
        else
        {
            p1.Y = panelSize.Height * MinAnchor.Y + p1.Y;
            p2.Y = panelSize.Height * MaxAnchor.Y - p2.Y;
        }

        var dist = p2 - p1;
        dist *= Alignment;
        var p1Final = p1 - dist;
        var p2Final = p2 - dist;
        var sizeFinal = p2Final - p1Final;

        widget.SetRelativeOffset(p1Final.Clone());
        widget.SetDrawSize(sizeFinal);
    }
}

/// <summary>
///     A container that draws children based on the settings provided in <see cref="PanelSlot" /> . Intended use is for
///     dock-able layouts or as a root for a collection of widgets
/// </summary>
public class Panel : Container<PanelSlot>
{
    public Panel(params Widget[] children) : base(children)
    {
        ClippingMode = Widgets.WidgetClippingMode.Bounds;
    }

    public override void Collect(WidgetFrame frame, DrawInfo info)
    {
        foreach (var slot in Slots)
        {
            var slotDrawInfo = info.AccountFor(slot.GetWidget());
            if (slotDrawInfo.Occluded) continue;
            slot.GetWidget().Collect(frame, info.AccountFor(slot.GetWidget()));
        }
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        foreach (var slot in Slots.ToArray()) slot.ComputeSizeAndOffset();
    }

    public override uint GetMaxSlots()
    {
        return 0;
    }

    protected override Size2d ComputeDesiredSize()
    {
        return new Size2d();
    }

    public override PanelSlot MakeSlot(Widget widget)
    {
        return new PanelSlot(this, widget);
    }

    public override void OnChildResized(Widget widget)
    {
        // Resize the specific widget
        Slots.Find(c => c.GetWidget() == widget)?.ComputeSizeAndOffset();
        //A canvas never needs to resize because of its children base.OnChildResized(widget);
    }
}