using System.Numerics;
using Rin.Framework.Extensions;
using Rin.Framework.Math;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Textures;
using Rin.Framework.Views.Font;

namespace Rin.Framework.Views.Graphics.Quads;

public static class QuadExtensions
{
    public static CommandList AddQuads(this CommandList commandList, params Quad[] quads)
    {
        return commandList.Add(new QuadDrawCommand(quads));
    }

    public static CommandList AddRect(this CommandList commandList, Matrix4x4 transform, in Vector2 size,
        in Color? color = null, in Vector4? borderRadius = null)
    {
        return commandList.AddQuads(Quad.Rect(transform, size, color, borderRadius));
    }

    public static CommandList AddCircle(this CommandList commandList, in Vector2 center, float radius,
        in Color? color = null)
    {
        return commandList.AddQuads(Quad.Circle(Matrix4x4.Identity.Translate(center), radius, color));
    }

    public static CommandList AddLine(this CommandList commandList, in Vector2 begin, in Vector2 end,
        float thickness = 2.0f,
        in Color? color = null)
    {
        return commandList.AddQuads(Quad.Line(begin, end, thickness, color));
    }

    public static CommandList AddQuadraticCurve(this CommandList commandList, in Vector2 begin, in Vector2 end,
        in Vector2 control,
        float thickness = 2.0f,
        in Color? color = null)
    {
        return commandList.AddQuads(Quad.QuadraticCurve(begin, control, end, thickness, color));
    }

    public static CommandList AddCubicCurve(this CommandList commandList, in Vector2 begin, in Vector2 end,
        in Vector2 controlA,
        in Vector2 controlB, float thickness = 2.0f, in Color? color = null)
    {
        return commandList.AddQuads(Quad.CubicCurve(begin, controlA, end, controlB, thickness, color));
    }

    public static CommandList AddQuadraticCurve(this CommandList commandList, in Matrix4x4 transform, in Vector2 begin,
        in Vector2 end,
        in Vector2 control, float thickness = 2.0f,
        in Color? color = null)
    {
        return commandList.AddQuadraticCurve(begin.Transform(transform), end.Transform(transform),
            control.Transform(transform), thickness, color);
    }

    public static CommandList AddCubicCurve(this CommandList commandList, in Matrix4x4 transform, in Vector2 begin,
        in Vector2 end,
        in Vector2 controlA,
        in Vector2 controlB, float thickness = 2.0f, in Color? color = null)
    {
        return commandList.AddCubicCurve(begin.Transform(transform), end.Transform(transform),
            controlA.Transform(transform), controlB.Transform(transform), thickness, color);
    }

    public static CommandList AddTexture(this CommandList commandList, in ImageHandle imageHandle,
        in Matrix4x4 transform,
        in Vector2 size, in Color? tint = null, in Vector4? uv = null,
        in Vector4? borderRadius = null)
    {
        return commandList.AddQuads(Quad.Texture(imageHandle, transform, size, tint, borderRadius, uv));
    }


    public static CommandList AddMtsdf(this CommandList commandList, in ImageHandle imageHandle, in Matrix4x4 transform,
        in Vector2 size, in Color? color = null, in Vector4? uv = null)
    {
        return commandList.AddQuads(Quad.Mtsdf(imageHandle, transform, size, color, uv));
    }

    public static CommandList AddText(this CommandList commandList,
        in Matrix4x4 transform, IFont font, ReadOnlySpan<char> text,
        float fontSize = 24f, in Color? color = null)
    {
        var fontManager = font.FontManager;
        var textureFactory = SGraphicsModule.Get().GetImageFactory();
        foreach (var bound in font.MeasureText(text, fontSize))
        {
            var range = fontManager.GetPixelRange();
            var glyph = fontManager.GetGlyph(font, bound.Character);

            if (glyph.State == LiveGlyphState.Invalid && bound.Character.IsPrintable())
                fontManager.Prepare(font, [bound.Character]);

            if (glyph.State != LiveGlyphState.Ready || !textureFactory.IsTextureReady(glyph.AtlasHandle)) continue;

            var charOffset = bound.Position;

            var size = bound.Size;
            var vectorSize = glyph.Size - new Vector2(range * 2);
            var scale = size / vectorSize;
            var pxRangeScaled = new Vector2(range) * scale;
            size += pxRangeScaled * 2;

            charOffset -= pxRangeScaled;

            var finalTransform = Matrix4x4.Identity.Scale(new Vector2(1.0f, -1.0f)).Translate(charOffset with
            {
                Y = charOffset.Y + size.Y
            });

            commandList.AddMtsdf(glyph.AtlasHandle, finalTransform * transform, size, color, glyph.Coordinate);
        }

        return commandList;
    }

    public static CommandList AddText(this CommandList commandList,
        in Matrix4x4 transform, string fontName, ReadOnlySpan<char> text,
        float fontSize = 24f, in Color? color = null)
    {
        if (SViewsModule.Get().GetFontManager().GetFont(fontName) is { } font)
            commandList.AddText(in transform, font, text, fontSize, in color);

        return commandList;
    }
}