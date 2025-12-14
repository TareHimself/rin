using System.Numerics;
using Rin.Framework.Graphics.Images;

namespace Rin.Framework.Views.Font;

public record struct LiveGlyphInfo
{
    public required ImageHandle AtlasHandle;
    public required LiveGlyphState State;
    public required Vector2 Size;
    public required Vector4 Coordinate;
}