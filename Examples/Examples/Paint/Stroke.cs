using System.Numerics;
using Rin.Core.Views;

namespace Examples.Paint;

/// <summary>
///     One continuous drag of the brush: the points the cursor passed through, in canvas space.
/// </summary>
public sealed class Stroke(Color color, float thickness)
{
    public Color Color { get; } = color;

    public float Thickness { get; } = thickness;

    public List<Vector2> Points { get; } = [];
}
