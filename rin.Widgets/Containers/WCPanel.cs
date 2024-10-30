using rin.Core.Math;

namespace rin.Widgets.Containers;

/// <summary>
///     Describes how a <see cref="Widget" /> will be sized and positioned in a <see cref="WCPanel" />. Supports Anchors
/// </summary>
public class PanelSlot : Slot
{
    public Vector2<float> Alignment = new(0, 0);
    public Vector2<float> MaxAnchor = new(0, 0);
    public Vector2<float> MinAnchor = new(0, 0);
    public Vector2<float> Offset = new(0, 0);
    public Size2d Size = new();

    /// <summary>
    ///     Describes how a <see cref="Widget" /> will be sized and positioned in a <see cref="WCPanel" />. Supports Anchors
    /// </summary>
    public PanelSlot(Widget widget,WCPanel? panel = null) : base(widget,panel)
    {
    }

    public bool SizeToContent { get; set; } = false;

    public static bool NearlyEqual(double a, double b, double tolerance = 0.001f)
    {
        return System.Math.Abs(a - b) < tolerance;
    }

    
}

/// <summary>
///     A container that draws children based on the settings provided in <see cref="PanelSlot" /> . Intended use is for
///     dock-able layouts or as a root for a collection of widgets
/// </summary>
public class WCPanel : Container
{
    public WCPanel(params Widget[] children) : base(children)
    {
    }
    
    protected override void ArrangeSlots(Size2d drawSize)
    {
        foreach (var slot in GetSlots())
        {
            OnSlotUpdated(slot);
        };
    }

    public override void OnSlotUpdated(Slot slot)
    {
        base.OnSlotUpdated(slot);
        if (slot is PanelSlot asPanelSlot)
        {
            var widget = slot.GetWidget();
            var panelSize = GetContentSize();

            var noOffsetX = PanelSlot.NearlyEqual(asPanelSlot.MinAnchor.X, asPanelSlot.MaxAnchor.X);
            var noOffsetY = PanelSlot.NearlyEqual(asPanelSlot.MinAnchor.Y, asPanelSlot.MaxAnchor.Y);

            var widgetSize = widget.GetDesiredSize();
            var wSize = new Size2d
            {
                Width = asPanelSlot.SizeToContent && noOffsetX ? widgetSize.Width : asPanelSlot.Size.Width,
                Height = asPanelSlot.SizeToContent && noOffsetY ? widgetSize.Height : asPanelSlot.Size.Height
            };

            var p1 = asPanelSlot.Offset.Clone();
            var p2 = p1 + wSize;

            if (noOffsetX)
            {
                var a = panelSize.Width * asPanelSlot.MinAnchor.X;
                p1.X += a;
                p2.X += a;
            }
            else
            {
                p1.X = panelSize.Width * asPanelSlot.MinAnchor.X + p1.X;
                p2.X = panelSize.Width * asPanelSlot.MaxAnchor.X - p2.X;
            }

            if (noOffsetY)
            {
                var a = panelSize.Height * asPanelSlot.MinAnchor.Y;
                p1.Y += a;
                p2.Y += a;
            }
            else
            {
                p1.Y = panelSize.Height * asPanelSlot.MinAnchor.Y + p1.Y;
                p2.Y = panelSize.Height * asPanelSlot.MaxAnchor.Y - p2.Y;
            }

            var dist = p2 - p1;
            dist *= asPanelSlot.Alignment;
            var p1Final = p1 - dist;
            var p2Final = p2 - dist;
            var sizeFinal = p2Final - p1Final;

            widget.SetOffset(p1Final.Clone());
            widget.SetSize(sizeFinal);
        }
    }

    
    protected override Size2d ComputeDesiredContentSize()
    {
        return new Size2d();
    }

    public override PanelSlot MakeSlot(Widget widget)
    {
        return new PanelSlot(widget,this);
    }
}