using System.Numerics;

namespace Rin.Core.Views.Font;

public struct GlyphRect
{
    public char Character;

    /// <summary>
    ///     This character's ink bounding box, in the same aspect ratio as its rasterized glyph - used for
    ///     rendering (see <see cref="Content.TextBoxView.ComputeLayout" />). A character with no ink (space,
    ///     NBSP, ...) has <see cref="Position" />/<see cref="Size" /> both zero, so this is unsuitable for caret
    ///     or hit-test positioning - use <see cref="PenX" />/<see cref="Advance" /> for that instead.
    /// </summary>
    public Vector2 Position;

    public Vector2 Size;

    /// <summary>
    ///     Pen position immediately before this character (i.e. where a caret placed "before" it belongs).
    ///     Advancing by <see cref="Advance" /> lands on the pen position immediately after it.
    /// </summary>
    public float PenX;

    /// <summary>
    ///     This character's horizontal advance - unlike <see cref="Size" />.X, this is defined even for glyphs
    ///     with no ink (space, NBSP, ...), so it's what caret/cursor and hit-test math should use, not Size.X.
    /// </summary>
    public float Advance;

    /// <summary>
    ///     Top of the line this character is on (shared by every character on that line, regardless of each
    ///     glyph's own ink extents) - use with <see cref="LineHeight" /> for caret placement, not
    ///     <see cref="Top" />/<see cref="Bottom" /> which vary per glyph.
    /// </summary>
    public float LineTop;

    public float LineHeight;

    public float Top => Position.Y;
    public float Left => Position.X;
    public float Right => Position.X + Size.X;
    public float Bottom => Position.Y + Size.Y;
}