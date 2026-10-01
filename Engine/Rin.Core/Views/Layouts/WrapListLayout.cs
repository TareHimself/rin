using System.Numerics;
using Rin.Core.Views.Composite;

namespace Rin.Core.Views.Layouts;

public class WrapListLayout(Axis axis, ICompositeView container) : ListLayout(axis, container)
{
    private readonly List<ListSlot> _line = [];

    public override Vector2 Apply(in Vector2 availableSpace)
    {
        var mainLimit = Main(availableSpace);
        var crossAvailable = Cross(availableSpace);
        var offsetMain = 0f;
        var offsetCross = 0f;
        var lineCross = 0f;
        var widestLine = 0f;

        _line.Clear();
        foreach (var slot in GetSlots())
        {
            if (slot is not ListSlot listSlot) continue;

            var size = listSlot.Child.Layout(Compose(float.PositiveInfinity, crossAvailable));
            var sizeMain = Main(size);

            if (_line.Count > 0 && offsetMain + sizeMain > mainLimit)
            {
                FinishLine(lineCross);
                offsetCross += lineCross;
                offsetMain = 0f;
                lineCross = 0f;
            }

            listSlot.Child.Offset = Compose(offsetMain, offsetCross);
            _line.Add(listSlot);
            offsetMain += sizeMain;
            lineCross = float.Max(lineCross, Cross(size));
            widestLine = float.Max(widestLine, offsetMain);
        }

        FinishLine(lineCross);
        return Compose(float.Min(widestLine, mainLimit), offsetCross + lineCross);
    }

    private void FinishLine(float lineCross)
    {
        foreach (var slot in _line) HandleCrossAxisOffset(slot, lineCross);
        _line.Clear();
    }

    private float Main(in Vector2 size)
    {
        return GetAxis() == Axis.Row ? size.X : size.Y;
    }

    private float Cross(in Vector2 size)
    {
        return GetAxis() == Axis.Row ? size.Y : size.X;
    }

    private Vector2 Compose(float main, float cross)
    {
        return GetAxis() == Axis.Row ? new Vector2(main, cross) : new Vector2(cross, main);
    }
}
