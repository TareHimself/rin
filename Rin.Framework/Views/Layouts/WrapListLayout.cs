using System.Numerics;
using Rin.Framework.Views.Composite;

namespace Rin.Framework.Views.Layouts;

public class WrapListLayout(Axis axis, CompositeView container) : ListLayout(axis, container)
{
    public override CompositeView Container { get; } = container;

    public override void OnSlotUpdated(ISlot slot)
    {
        if (Container.Surface != null) Apply(Container.GetContentSize());
    }

    public override Vector2 Apply(Vector2 availableSpace)
    {
        var axis = GetAxis();
        var availableMain = axis switch
        {
            Axis.Row => availableSpace.X,
            Axis.Column => availableSpace.Y,
            _ => throw new ArgumentOutOfRangeException()
        };
        var offsetMain = 0.0f;
        var offsetCross = 0.0f;
        var currentMaxSizeCross = 0.0f;
        var totalSizeMain = 0.0f;
        var totalSizeCross = 0.0f;
        List<ListSlot> currentLine = [];
        foreach (var slot in GetSlots())
        {
            slot.Child.ComputeSize(availableSpace with { X = float.PositiveInfinity });
            var slotSizeMain = axis switch
            {
                Axis.Row => slot.Child.Size.X,
                Axis.Column => slot.Child.Size.Y,
                _ => throw new ArgumentOutOfRangeException()
            };
            var slotSizeCross = axis switch
            {
                Axis.Row => slot.Child.Size.Y,
                Axis.Column => slot.Child.Size.X,
                _ => throw new ArgumentOutOfRangeException()
            };
            var projectedSlotEndMain = offsetMain + slotSizeMain;
            float finalSlotOffsetMain;
            if (projectedSlotEndMain > availableMain)
            {
                // If it does not fit on this line go to the next line
                if (projectedSlotEndMain >= availableMain && offsetMain >= 0.0f)
                {
                    foreach (var listSlot in currentLine) HandleCrossAxisOffset(listSlot, currentMaxSizeCross);
                    currentLine.Clear();
                    offsetMain = 0.0f;
                    offsetCross += currentMaxSizeCross;
                    currentMaxSizeCross = 0.0f;
                }

                finalSlotOffsetMain = offsetMain;
            }
            else
            {
                finalSlotOffsetMain = projectedSlotEndMain - slotSizeMain;
            }

            currentLine.Add(slot as ListSlot ?? throw new InvalidOperationException());
            offsetMain += slotSizeMain;
            var finalSlotOffsetCross = offsetCross;
            currentMaxSizeCross = float.Max(currentMaxSizeCross, slotSizeCross);
            var finalSlotEndMain = finalSlotOffsetMain + slotSizeMain;
            var finalSlotEndCross = finalSlotOffsetCross + slotSizeCross;
            totalSizeMain = float.Max(totalSizeMain, float.Min(availableMain, finalSlotEndMain));
            totalSizeCross = float.Max(totalSizeCross, finalSlotEndCross);


            slot.Child.Offset = axis switch
            {
                Axis.Row => new Vector2(finalSlotOffsetMain, finalSlotOffsetCross),
                Axis.Column => new Vector2(finalSlotOffsetCross, finalSlotOffsetMain),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return new Vector2(totalSizeMain, totalSizeCross);
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return GetAxis() switch
        {
            Axis.Row => GetSlots().Aggregate(new Vector2(), (size, slot) =>
            {
                var slotSize = slot.Child.GetDesiredSize();
                size.X += slotSize.X;
                size.Y = float.Max(size.Y, slotSize.Y);
                return size;
            }),
            Axis.Column => GetSlots().Aggregate(new Vector2(), (size, slot) =>
            {
                var slotSize = slot.Child.GetDesiredSize();
                size.Y += slotSize.Y;
                size.X = float.Max(size.X, slotSize.X);
                return size;
            }),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}