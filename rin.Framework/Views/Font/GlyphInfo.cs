using System.Numerics;

namespace rin.Framework.Views.Font;

public struct GlyphInfo : IEquatable<GlyphInfo>
{
    public required int AtlasId;
    public required GlyphState State;
    public required Vector2 Size;
    public required Vector4 Coordinate;

    public bool Equals(GlyphInfo other)
    {
        return AtlasId == other.AtlasId && State == other.State && Size.Equals(other.Size) &&
               Coordinate.Equals(other.Coordinate);
    }

    public override bool Equals(object? obj)
    {
        return obj is GlyphInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(AtlasId, (int)State, Size, Coordinate);
    }
}