using System.Numerics;
using Rin.Core.Views.Font;

namespace Rin.Core.Views.Content;

/// <summary>
///     Caret math over measured text. A caret index is the character the caret sits after, -1 for the start.
/// </summary>
public static class TextCaretLayout
{
    /// <summary>
    ///     The caret index nearest to a point in text space.
    /// </summary>
    public static int IndexAt(ReadOnlySpan<GlyphRect> bounds, Vector2 point)
    {
        if (bounds.IsEmpty) return -1;

        var lineStart = FindLineStart(bounds, point.Y);
        var lineTop = bounds[lineStart].LineTop;
        for (var i = lineStart; i < bounds.Length; i++)
        {
            var bound = bounds[i];
            if (bound.LineTop != lineTop) return i - 1;
            if (bound.Character == '\n' || point.X < bound.PenX + bound.Advance / 2f) return i - 1;
        }

        return bounds.Length - 1;
    }

    /// <summary>
    ///     The scroll along one axis that brings the span from start to end into the view.
    /// </summary>
    public static float ScrollToShow(float scroll, float start, float end, float viewSize, float textSize)
    {
        if (start < scroll) scroll = start;
        else if (end > scroll + viewSize) scroll = end - viewSize;
        return ClampScroll(scroll, viewSize, textSize);
    }

    /// <summary>
    ///     Keeps a scroll along one axis from showing space before or past the text.
    /// </summary>
    public static float ClampScroll(float scroll, float viewSize, float textSize)
    {
        return float.Clamp(scroll, 0f, float.Max(0f, textSize - viewSize));
    }

    /// <summary>
    ///     The width of the widest line and the height of every line, counting trailing whitespace and the
    ///     empty line after a trailing line break.
    /// </summary>
    public static Vector2 Size(ReadOnlySpan<GlyphRect> bounds)
    {
        if (bounds.IsEmpty) return Vector2.Zero;

        var width = 0f;
        foreach (var bound in bounds) width = float.Max(width, bound.PenX + bound.Advance);
        var last = bounds[^1];
        var lines = last.Character == '\n' ? 2f : 1f;
        return new Vector2(width, last.LineTop + last.LineHeight * lines);
    }

    private static int FindLineStart(ReadOnlySpan<GlyphRect> bounds, float y)
    {
        var lineStart = 0;
        for (var i = 0; i < bounds.Length; i++)
        {
            if (bounds[i].LineTop != bounds[lineStart].LineTop) lineStart = i;
            if (y < bounds[i].LineTop + bounds[i].LineHeight) break;
        }

        return lineStart;
    }
}
