using System.Numerics;
using Rin.Shade;

namespace Rin.Core.Views.Graphics.Shaders;

public static class ViewShaderMath
{
    // https://www.shadertoy.com/view/fsdyzB
    public static float SdRoundedBox(Vector2 p, Vector2 halfSize, BorderRadius radius)
    {
        var cornerRadius = p.X > 0f
            ? p.Y > 0f ? radius.BottomRight : radius.TopRight
            : p.Y > 0f ? radius.BottomLeft : radius.TopLeft;

        var q = Shader.Abs(p) - halfSize + new Vector2(cornerRadius);
        return Shader.Min(Shader.Max(q.X, q.Y), 0f) +
               Shader.Length(Shader.Max(q, new Vector2(0f))) - cornerRadius;
    }

    public static Vector2 TransformPoint(Vector2 point, Matrix4x4 matrix)
    {
        return Vector4.Transform(new Vector4(point, 0f, 1f), matrix).XY;
    }

    public static float Median(float r, float g, float b)
    {
        return Shader.Max(Shader.Min(r, g), Shader.Min(Shader.Max(r, g), b));
    }

    public static float ScreenPxRange(Vector2 uv, Vector2 size, float pixelRange)
    {
        var unitRange = new Vector2(pixelRange) / size;
        var screenTexSize = new Vector2(1f) / Shader.Fwidth(uv);
        return Shader.Max(0.5f * Shader.Dot(unitRange, screenTexSize), 1f);
    }

    public static Vector3 Hsv2Rgb(Vector3 color)
    {
        var k = new Vector4(1f, 2f / 3f, 1f / 3f, 3f);
        var p = Shader.Abs(Shader.Frac(new Vector3(color.X) + k.XYZ) * 6f - new Vector3(k.W));
        return color.Z * Shader.Lerp(new Vector3(k.X), Shader.Clamp(p - new Vector3(k.X), new Vector3(0f), new Vector3(1f)), color.Y);
    }

    /// <summary>
    ///     Fades <paramref name="color" /> to transparent outside a rounded rectangle of <paramref name="size" />
    ///     placed by the inverse of its transform.
    /// </summary>
    public static Vector4 ApplyBorderRadius(Vector2 fragPosition, Vector4 color, BorderRadius radius, Vector2 size,
        Matrix4x4 inverseTransform, float transition)
    {
        var transformedFrag = Vector4.Transform(new Vector4(fragPosition, 0f, 1f), inverseTransform).XY;

        var halfSize = size / 2f;
        var distance = SdRoundedBox(transformedFrag - halfSize, halfSize, radius);
        var smoothedAlpha = Shader.SmoothStep(0f, transition, distance);

        return Shader.Lerp(color, new Vector4(color.XYZ, 0f), smoothedAlpha);
    }
}
