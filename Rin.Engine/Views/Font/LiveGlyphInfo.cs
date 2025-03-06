using System.Numerics;

namespace Rin.Engine.Views.Font;

public struct LiveGlyphInfo : IEquatable<LiveGlyphInfo>
{
    public required int AtlasId;
    public required LiveGlyphState State;
    public required Vector2 Size;
    public required Vector4 Coordinate;

    public bool Equals(LiveGlyphInfo other)
    {
        return AtlasId == other.AtlasId && State == other.State && Size.Equals(other.Size) &&
               Coordinate.Equals(other.Coordinate);
    }

    public override bool Equals(object? obj)
    {
        return obj is LiveGlyphInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(AtlasId, (int)State, Size, Coordinate);
    }
}