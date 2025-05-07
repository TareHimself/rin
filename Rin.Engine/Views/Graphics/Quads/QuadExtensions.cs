using System.Numerics;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.Math;

namespace Rin.Engine.Views.Graphics.Quads;

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

    public static CommandList AddTexture(this CommandList commandList, in TextureHandle textureHandle, in Matrix4x4 transform,
        in Vector2 size, in Color? tint = null, in Vector4? uv = null,
        in Vector4? borderRadius = null)
    {
        return commandList.AddQuads(Quad.Texture(textureHandle, transform, size, tint, borderRadius, uv));
    }


    public static CommandList AddMtsdf(this CommandList commandList, in TextureHandle textureHandle, in Matrix4x4 transform,
        in Vector2 size, in Color? color = null, in Vector4? uv = null)
    {
        return commandList.AddQuads(Quad.Mtsdf(textureHandle, transform, size, color, uv));
    }
}