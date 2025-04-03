using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Extensions;
using Rin.Engine.Views.Composite;

namespace Rin.Engine.Views.Layouts;

/// <summary>
///     Describes how a <see cref="View" /> will be sized and positioned in a <see cref="Panel" />. Supports Anchors
///     Slot = <see cref="PanelSlot" />
/// </summary>
public class PanelSlot : Slot
{
    [PublicAPI] public Vector2 Alignment;

    [PublicAPI] public Vector2 MaxAnchor;

    [PublicAPI] public Vector2 MinAnchor;

    [PublicAPI] public Vector2 Offset;

    [PublicAPI] public Vector2 Size;

    /// <summary>
    ///     Describes how a <see cref="View" /> will be sized and positioned in a <see cref="PanelLayout" />. Supports Anchors
    /// </summary>
    public PanelSlot(PanelLayout? panel = null) : base(panel)
    {
    }

    public bool SizeToContent { get; set; } = false;

    public static bool NearlyEqual(float a, float b, float tolerance = 0.001f)
    {
        return float.Abs(a - b) < tolerance;
    }
}

public class PanelLayout(CompositeView container) : InfiniteChildrenLayout
{
    public override CompositeView Container { get; } = container;

    public override void Dispose()
    {
    }

    public override ISlot MakeSlot(View view)
    {
        throw new NotImplementedException();
    }

    public override void OnSlotUpdated(ISlot slot)
    {
        if (Container.Surface != null) LayoutSlot(slot, Container.GetContentSize());
    }

    public override Vector2 Apply(Vector2 availableSpace)
    {
        foreach (var slot in GetSlots()) LayoutSlot(slot, availableSpace);
        ;

        return availableSpace;
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2();
    }

    private void LayoutSlot(ISlot slot, Vector2 panelSize)
    {
        if (slot is PanelSlot asPanelSlot)
        {
            var view = slot.Child;

            var absoluteX = PanelSlot.NearlyEqual(asPanelSlot.MinAnchor.X, asPanelSlot.MaxAnchor.X);
            var absoluteY = PanelSlot.NearlyEqual(asPanelSlot.MinAnchor.Y, asPanelSlot.MaxAnchor.Y);

            var desiredSize = view.GetDesiredSize();
            var areBothRelative = absoluteX == absoluteY && absoluteX == false;
            // The size we assume the widget is for offset calculations
            var workingSize = asPanelSlot.SizeToContent && !areBothRelative
                ? view.ComputeSize(new Vector2(float.PositiveInfinity))
                : asPanelSlot.Size;
            // new Vector2
            // {
            //     X = asPanelSlot.SizeToContent && noOffsetX ? desiredSize.X : asPanelSlot.Size.X,
            //     Y = asPanelSlot.SizeToContent && noOffsetY ? desiredSize.Y: asPanelSlot.Size.Y
            // };

            var p1 = asPanelSlot.Offset.Clone();
            var p2 = p1 + workingSize;

            if (absoluteX)
            {
                var delta = panelSize.X * asPanelSlot.MinAnchor.X;
                p1.X += delta;
                p2.X += delta;
            }
            else
            {
                p1.X = panelSize.X * asPanelSlot.MinAnchor.X;
                p2.X = panelSize.X * asPanelSlot.MaxAnchor.X;
            }

            if (absoluteY)
            {
                var delta = panelSize.Y * asPanelSlot.MinAnchor.Y;
                p1.Y += delta;
                p2.Y += delta;
            }
            else
            {
                p1.Y = panelSize.Y * asPanelSlot.MinAnchor.Y;
                p2.Y = panelSize.Y * asPanelSlot.MaxAnchor.Y;
            }

            var dist = p2 - p1;
            dist *= asPanelSlot.Alignment;
            var p1Final = p1 - dist;
            var p2Final = p2 - dist;
            var sizeFinal = p2Final - p1Final;

            view.Offset = p1Final;
            if (workingSize != sizeFinal) view.ComputeSize(sizeFinal);
        }
    }
}