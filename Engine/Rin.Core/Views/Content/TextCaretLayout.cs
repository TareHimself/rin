using System.Numerics;
using Rin.Core.Graphics;
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

    /// <summary>
    ///     One rect per line covering the characters after caret index <paramref name="start" /> up to and
    ///     including the one at <paramref name="end" />. A selected line break shows as a quarter line of width.
    /// </summary>
    public static void SelectionRects(ReadOnlySpan<GlyphRect> bounds, int start, int end, List<Rect2D> output)
    {
        output.Clear();
        var first = int.Max(start + 1, 0);
        var last = int.Min(end, bounds.Length - 1);
        if (first > last) return;

        var lineStart = first;
        for (var i = first; i <= last; i++)
        {
            var isLineEnd = i == last || bounds[i + 1].LineTop != bounds[i].LineTop;
            if (!isLineEnd) continue;

            var left = bounds[lineStart].PenX;
            var right = bounds[i].PenX + SelectedWidth(bounds[i]);
            output.Add(new Rect2D(new Vector2(left, bounds[i].LineTop), new Vector2(right - left, bounds[i].LineHeight)));
            lineStart = i + 1;
        }
    }

    private static float SelectedWidth(in GlyphRect bound)
    {
        return bound.Character == '\n' ? float.Max(bound.Advance, bound.LineHeight / 4f) : bound.Advance;
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
