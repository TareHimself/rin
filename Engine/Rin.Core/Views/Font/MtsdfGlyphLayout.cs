using System.Numerics;
using Rin.Core.Shared.Math;

namespace Rin.Core.Views.Font;

/// <summary>
///     Shared quad-placement math for a single MTSDF glyph - the atlas rect stores extra padding
///     (<paramref name="pixelRange" /> on every side, baked in at generation time so the SDF has room to
///     anti-alias), so the quad has to be grown/offset by that padding (scaled to the glyph's rendered size) to
///     land exactly on the glyph's ink bounds.
/// </summary>
public static class MtsdfGlyphLayout
{
    public static (Matrix4x4 Transform, Vector2 Size) Compute(in GlyphRect bound, in LiveGlyphInfo glyph,
        float pixelRange)
    {
        var offset = bound.Position;

        var size = bound.Size;
        var vectorSize = glyph.Size - new Vector2(pixelRange * 2);
        var scale = size / vectorSize;
        var pxRangeScaled = new Vector2(pixelRange) * scale;
        size += pxRangeScaled * 2;

        offset -= pxRangeScaled;

        var transform = Matrix4x4.Identity.Scale(new Vector2(1.0f, -1.0f)).Translate(offset with
        {
            Y = offset.Y + size.Y
        });

        return (transform, size);
    }
}
