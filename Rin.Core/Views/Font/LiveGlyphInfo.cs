using System.Numerics;
using Rin.Core.Graphics.Images;

namespace Rin.Core.Views.Font;

public record struct LiveGlyphInfo
{
    public required ImageHandle AtlasHandle;
    public required Vector4 Coordinate;
    public required Vector2 Size;
    public required LiveGlyphState State;
}