﻿using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Enums;

namespace rin.Framework.Views.Layouts;

/// <summary>
/// Describes how a <see cref="View" /> will be sized and positioned in a <see cref="Panel" />. Supports Anchors
/// Slot = <see cref="PanelSlot"/>
/// </summary>
public class PanelSlot : Slot
{
    public Vec2<float> Alignment = new(0, 0);
    public Vec2<float> MaxAnchor = new(0, 0);
    public Vec2<float> MinAnchor = new(0, 0);
    public Vec2<float> Offset = new(0, 0);
    public Vec2<float> Size = new();

    /// <summary>
    ///     Describes how a <see cref="View" /> will be sized and positioned in a <see cref="PanelLayout" />. Supports Anchors
    /// </summary>
    public PanelSlot(PanelLayout? panel = null) : base(panel)
    {
    }

    public bool SizeToContent { get; set; } = false;

    public static bool NearlyEqual(double a, double b, double tolerance = 0.001f)
    {
        return System.Math.Abs(a - b) < tolerance;
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
        if (Container.Surface != null)
        {
            LayoutSlot(slot,Container.GetContentSize());
        }
    }

    public override Vec2<float> Apply(Vec2<float> availableSpace)
    {
        foreach (var slot in GetSlots())
        {
            LayoutSlot(slot,availableSpace);
        };
        
        return availableSpace;
    }

    public override Vec2<float> ComputeDesiredContentSize()
    {
        return 0.0f;
    }

    private void LayoutSlot(ISlot slot,Vec2<float> panelSize)
    {
        if (slot is PanelSlot asPanelSlot)
        {
            var view = slot.Child;

            var noOffsetX = PanelSlot.NearlyEqual(asPanelSlot.MinAnchor.X, asPanelSlot.MaxAnchor.X);
            var noOffsetY = PanelSlot.NearlyEqual(asPanelSlot.MinAnchor.Y, asPanelSlot.MaxAnchor.Y);

            var viewSize = view.GetDesiredSize();
            
            var wSize = new Vec2<float>
            {
                X = asPanelSlot.SizeToContent && noOffsetX ? viewSize.X : asPanelSlot.Size.X,
                Y = asPanelSlot.SizeToContent && noOffsetY ? viewSize.Y: asPanelSlot.Size.Y
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

            view.Offset = p1Final;
            view.ComputeSize(sizeFinal);
        }
    }
}