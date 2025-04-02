using System.Numerics;
using Rin.Engine.Core.Math;

namespace Rin.Engine.Views.Graphics.Quads;

public static class QuadExtensions
{
    public static PassCommands AddQuads(this PassCommands passCommands, params Quad[] quads)
    {
        return passCommands.Add(new QuadDrawCommand(quads));
    }

    public static PassCommands AddRect(this PassCommands passCommands, Matrix4x4 transform, Vector2 size,
        Vector4? color = null, Vector4? borderRadius = null)
    {
        return passCommands.AddQuads(Quad.Rect(transform, size, color, borderRadius));
    }

    public static PassCommands AddCircle(this PassCommands passCommands, Vector2 center, float radius,
        Vector4? color = null)
    {
        return passCommands.AddQuads(Quad.Circle(Matrix4x4.Identity.Translate(center), radius, color));
    }

    public static PassCommands AddLine(this PassCommands passCommands, Vector2 begin, Vector2 end,
        float thickness = 2.0f,
        Vector4? color = null)
    {
        return passCommands.AddQuads(Quad.Line(begin, end, thickness, color));
    }

    public static PassCommands AddQuadraticCurve(this PassCommands passCommands, Vector2 begin, Vector2 end,
        Vector2 control,
        float thickness = 2.0f,
        Vector4? color = null)
    {
        return passCommands.AddQuads(Quad.QuadraticCurve(begin, control, end, thickness, color));
    }

    public static PassCommands AddCubicCurve(this PassCommands passCommands, Vector2 begin, Vector2 end,
        Vector2 controlA,
        Vector2 controlB, float thickness = 2.0f, Vector4? color = null)
    {
        return passCommands.AddQuads(Quad.CubicCurve(begin, controlA, end, controlB, thickness, color));
    }

    public static PassCommands AddQuadraticCurve(this PassCommands passCommands, Matrix4x4 transform, Vector2 begin,
        Vector2 end,
        Vector2 control, float thickness = 2.0f,
        Vector4? color = null)
    {
        return passCommands.AddQuadraticCurve(begin.Transform(transform), end.Transform(transform),
            control.Transform(transform), thickness, color);
    }

    public static PassCommands AddCubicCurve(this PassCommands passCommands, Matrix4x4 transform, Vector2 begin,
        Vector2 end,
        Vector2 controlA,
        Vector2 controlB, float thickness = 2.0f, Vector4? color = null)
    {
        return passCommands.AddCubicCurve(begin.Transform(transform), end.Transform(transform),
            controlA.Transform(transform), controlB.Transform(transform), thickness, color);
    }

    public static PassCommands AddTexture(this PassCommands passCommands, int textureId, Matrix4x4 transform,
        Vector2 size, Vector4? tint = null, Vector4? uv = null,
        Vector4? borderRadius = null)
    {
        return passCommands.AddQuads(Quad.Texture(textureId, transform, size, tint, borderRadius, uv));
    }


    public static PassCommands AddMtsdf(this PassCommands passCommands, int textureId, Matrix4x4 transform,
        Vector2 size, Vector4? color = null, Vector4? uv = null)
    {
        return passCommands.AddQuads(Quad.Mtsdf(textureId, transform, size, color, uv));
    }
}