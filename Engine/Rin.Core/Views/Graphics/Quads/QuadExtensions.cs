using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Views.Font;
using Rin.Core.Extensions;
using Rin.Core.Shared.Math;

namespace Rin.Core.Views.Graphics.Quads;

public static class QuadExtensions
{
    public static CommandList AddQuads(this CommandList commandList, params ReadOnlySpan<Quad> quads)
    {
        return commandList.Add(new QuadDrawCommand(quads));
    }

    public static CommandList AddRect(this CommandList commandList, Matrix4x4 transform, in Vector2 size,
        in Color? color = null, in BorderRadius? borderRadius = null)
    {
        return commandList.AddQuads(Quad.Rect(transform, size, color, borderRadius));
    }

    /// <summary>
    ///     A circle centred on <paramref name="center" /> in surface space.
    /// </summary>
    public static CommandList AddCircle(this CommandList commandList, in Vector2 center, float radius,
        in Color? color = null)
    {
        return commandList.AddQuads(Quad.Circle(Matrix4x4.Identity.Translate(center - new Vector2(radius)), radius,
            color));
    }

    /// <summary>
    ///     A circle centred on the origin of <paramref name="transform" />.
    /// </summary>
    public static CommandList AddCircle(this CommandList commandList, in Matrix4x4 transform, float radius,
        in Color? color = null)
    {
        return commandList.AddCircle(transform, Vector2.Zero, radius, color);
    }

    /// <summary>
    ///     A circle centred on <paramref name="center" /> in the space of <paramref name="transform" />.
    /// </summary>
    public static CommandList AddCircle(this CommandList commandList, in Matrix4x4 transform, in Vector2 center,
        float radius,
        in Color? color = null)
    {
        return commandList.AddQuads(Quad.Circle(
            transform.ApplyBefore(Matrix4x4.Identity.Translate(center - new Vector2(radius))), radius, color));
    }

    /// <summary>
    ///     A circle whose bounding box has its top left corner at the origin of <paramref name="transform" />,
    ///     the way <see cref="AddRect" /> places a rect.
    /// </summary>
    public static CommandList AddCircleTopLeft(this CommandList commandList, in Matrix4x4 transform, float radius,
        in Color? color = null)
    {
        return commandList.AddQuads(Quad.Circle(transform, radius, color));
    }

    /// <summary>
    ///     A circle whose bounding box has its top left corner at <paramref name="topLeft" /> in the space of
    ///     <paramref name="transform" />.
    /// </summary>
    public static CommandList AddCircleTopLeft(this CommandList commandList, in Matrix4x4 transform,
        in Vector2 topLeft, float radius, in Color? color = null)
    {
        return commandList.AddQuads(Quad.Circle(transform.ApplyBefore(Matrix4x4.Identity.Translate(topLeft)),
            radius, color));
    }

    public static CommandList AddLine(this CommandList commandList, in Matrix4x4 transform, in Vector2 begin,
        in Vector2 end,
        float thickness = 2.0f,
        in Color? color = null)
    {
        return commandList.AddQuads(Quad.Line(transform, begin, end, thickness, color));
    }

    public static CommandList AddQuadraticCurve(this CommandList commandList, in Matrix4x4 transform, in Vector2 begin,
        in Vector2 end,
        in Vector2 control,
        float thickness = 2.0f,
        in Color? color = null)
    {
        return commandList.AddQuads(Quad.QuadraticCurve(transform, begin, control, end, thickness, color));
    }

    public static CommandList AddCubicCurve(this CommandList commandList, in Matrix4x4 transform, in Vector2 begin,
        in Vector2 end,
        in Vector2 controlA,
        in Vector2 controlB, float thickness = 2.0f, in Color? color = null)
    {
        return commandList.AddQuads(Quad.CubicCurve(transform, begin, controlA, end, controlB, thickness, color));
    }

    /// <summary>
    ///     A rect blending from <paramref name="from" /> at <paramref name="begin" /> to <paramref name="to" /> at
    ///     <paramref name="end" />, both in the rect's 0 to 1 UV space. The default runs top to bottom.
    /// </summary>
    public static CommandList AddGradient(this CommandList commandList, in Matrix4x4 transform, in Vector2 size,
        in Color from, in Color to, in Vector2? begin = null, in Vector2? end = null,
        in BorderRadius? borderRadius = null)
    {
        return commandList.AddQuads(Quad.Gradient(transform, size, from, to, begin ?? Vector2.Zero,
            end ?? Vector2.UnitY, borderRadius));
    }

    public static CommandList AddTexture(this CommandList commandList, in ResourceHandle imageHandle,
        in Matrix4x4 transform,
        in Vector2 size, in Color? tint = null, in Vector4? uv = null,
        in BorderRadius? borderRadius = null)
    {
        return commandList.Add(new QuadDrawCommand([Quad.Texture(imageHandle, transform, size, tint, borderRadius, uv)],
            [imageHandle]));
    }


    public static CommandList AddMtsdf(this CommandList commandList, in ResourceHandle imageHandle, in Matrix4x4 transform,
        in Vector2 size, float pixelRange, in Color? color = null, in Vector4? uv = null)
    {
        return commandList.Add(new QuadDrawCommand([Quad.Mtsdf(imageHandle, transform, size, pixelRange, color, uv)],
            [imageHandle]));
    }

    public static CommandList AddText(this CommandList commandList,
        in Matrix4x4 transform, IFont font, ReadOnlySpan<char> text,
        float fontSize = 24f, in Color? color = null)
    {
        var fontManager = font.FontManager;
        foreach (var bound in font.MeasureText(text, fontSize))
        {
            var range = fontManager.GetPixelRange();
            var glyph = fontManager.GetGlyph(font, bound.Character);

            if (glyph.State == LiveGlyphState.Invalid && bound.Character.IsPrintable())
                fontManager.Prepare(font, [bound.Character]);

            if (glyph.State != LiveGlyphState.Ready) continue;

            var (finalTransform, size) = MtsdfGlyphLayout.Compute(bound, glyph, range);

            commandList.AddMtsdf(glyph.AtlasHandle, finalTransform * transform, size, range, color, glyph.Coordinate);
        }

        return commandList;
    }

    public static CommandList AddText(this CommandList commandList,
        in Matrix4x4 transform, string fontName, ReadOnlySpan<char> text,
        float fontSize = 24f, in Color? color = null)
    {
        if (IViewsModule.Get().FontManager.GetFont(fontName) is { } font)
            commandList.AddText(in transform, font, text, fontSize, in color);

        return commandList;
    }
}