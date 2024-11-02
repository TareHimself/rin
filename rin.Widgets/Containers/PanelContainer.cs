using rin.Core.Math;

namespace rin.Widgets.Containers;

/// <summary>
/// Describes how a <see cref="Widget" /> will be sized and positioned in a <see cref="PanelContainer" />. Supports Anchors
/// Slot = <see cref="PanelContainerSlot"/>
/// </summary>
public class PanelContainerSlot : ContainerSlot
{
    public Vector2<float> Alignment = new(0, 0);
    public Vector2<float> MaxAnchor = new(0, 0);
    public Vector2<float> MinAnchor = new(0, 0);
    public Vector2<float> Offset = new(0, 0);
    public Vector2<float> Size = new();

    /// <summary>
    ///     Describes how a <see cref="Widget" /> will be sized and positioned in a <see cref="PanelContainer" />. Supports Anchors
    /// </summary>
    public PanelContainerSlot(PanelContainer? panel = null) : base(panel)
    {
    }

    public bool SizeToContent { get; set; } = false;

    public static bool NearlyEqual(double a, double b, double tolerance = 0.001f)
    {
        return System.Math.Abs(a - b) < tolerance;
    }

    
}

/// <summary>
///     A container that draws children based on the settings provided in <see cref="PanelContainerSlot" /> . Intended use is for
///     dock-able layouts or as a root for a collection of widgets
/// </summary>
public class PanelContainer : Container
{
    protected override void ArrangeSlots(Vector2<float> drawSize)
    {
        foreach (var slot in GetSlots())
        {
            OnSlotUpdated(slot);
        };
    }

    public override void OnSlotUpdated(ContainerSlot slot)
    {
        base.OnSlotUpdated(slot);
        if (slot is PanelContainerSlot asPanelSlot)
        {
            var widget = slot.Child;
            var panelSize = GetContentSize();

            var noOffsetX = PanelContainerSlot.NearlyEqual(asPanelSlot.MinAnchor.X, asPanelSlot.MaxAnchor.X);
            var noOffsetY = PanelContainerSlot.NearlyEqual(asPanelSlot.MinAnchor.Y, asPanelSlot.MaxAnchor.Y);

            var widgetSize = widget.GetDesiredSize();
            var wSize = new Vector2<float>
            {
               X = asPanelSlot.SizeToContent && noOffsetX ? widgetSize.X : asPanelSlot.Size.X,
                Y = asPanelSlot.SizeToContent && noOffsetY ? widgetSize.Y: asPanelSlot.Size.Y
            };

            var p1 = asPanelSlot.Offset.Clone();
            var p2 = p1 + wSize;

            if (noOffsetX)
            {
                var a = panelSize.X * asPanelSlot.MinAnchor.X;
                p1.X += a;
                p2.X += a;
            }
            else
            {
                p1.X = panelSize.X * asPanelSlot.MinAnchor.X + p1.X;
                p2.X = panelSize.X * asPanelSlot.MaxAnchor.X - p2.X;
            }

            if (noOffsetY)
            {
                var a = panelSize.Y * asPanelSlot.MinAnchor.Y;
                p1.Y += a;
                p2.Y += a;
            }
            else
            {
                p1.Y = panelSize.Y * asPanelSlot.MinAnchor.Y + p1.Y;
                p2.Y = panelSize.Y * asPanelSlot.MaxAnchor.Y - p2.Y;
            }

            var dist = p2 - p1;
            dist *= asPanelSlot.Alignment;
            var p1Final = p1 - dist;
            var p2Final = p2 - dist;
            var sizeFinal = p2Final - p1Final;

            widget.Offset = (p1Final.Clone());
            widget.Size = (sizeFinal);
        }
    }

    
    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return new Vector2<float>();
    }

    public override PanelContainerSlot MakeSlot(Widget widget)
    {
        return new PanelContainerSlot(this)
        {
            Child = widget
        };
    }
}