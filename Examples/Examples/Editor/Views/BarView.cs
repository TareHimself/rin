using System.Numerics;
using Examples.Common.Views;

namespace Examples.Editor.Views;

/// <summary>
///     A card that spans the whole width it is given, since a plain rect is only as wide as its content.
/// </summary>
public class BarView : CardView
{
    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        var size = base.ArrangeContent(availableSpace);
        return float.IsFinite(availableSpace.X) ? size with { X = availableSpace.X } : size;
    }
}
