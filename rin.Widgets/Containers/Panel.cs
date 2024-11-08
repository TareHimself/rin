using rin.Core.Math;

namespace rin.Widgets.Containers;

/// <summary>
/// Describes how a <see cref="Widget" /> will be sized and positioned in a <see cref="Panel" />. Supports Anchors
/// Slot = <see cref="PanelSlot"/>
/// </summary>
public class PanelSlot : ContainerSlot
{
    public Vector2<float> Alignment = new(0, 0);
    public Vector2<float> MaxAnchor = new(0, 0);
    public Vector2<float> MinAnchor = new(0, 0);
    public Vector2<float> Offset = new(0, 0);
    public Vector2<float> Size = new();

    /// <summary>
    ///     Describes how a <see cref="Widget" /> will be sized and positioned in a <see cref="Panel" />. Supports Anchors
    /// </summary>
    public PanelSlot(Panel? panel = null) : base(panel)
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
public class Panel : Container
{
    // protected override void ArrangeContent(Vector2<float> drawSize)
    // {
    //     foreach (var slot in GetSlots())
    //     {
    //         OnSlotUpdated(slot);
    //     };
    // }


    protected override Vector2<float> ArrangeContent(Vector2<float> availableSpace)
    {
        foreach (var slot in GetSlots())
        {
            LayoutSlot(slot,availableSpace);
        };
        
        return availableSpace;
    }

    protected void LayoutSlot(ContainerSlot slot,Vector2<float> panelSize)
    {
        if (slot is PanelSlot asPanelSlot)
        {
            var widget = slot.Child;

            var noOffsetX = PanelSlot.NearlyEqual(asPanelSlot.MinAnchor.X, asPanelSlot.MaxAnchor.X);
            var noOffsetY = PanelSlot.NearlyEqual(asPanelSlot.MinAnchor.Y, asPanelSlot.MaxAnchor.Y);

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

            widget.Offset = p1Final;
            widget.ComputeSize(sizeFinal);
        }
    }
    public override void OnSlotUpdated(ContainerSlot slot)
    {
        base.OnSlotUpdated(slot);
        LayoutSlot(slot,GetContentSize());
    }

    
    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return new Vector2<float>();
    }

    protected override PanelSlot MakeSlot(Widget widget)
    {
        return new PanelSlot(this)
        {
            Child = widget
        };
    }
}