using System.Numerics;
using Rin.Shade;

namespace Rin.Core.Views.Graphics.Shaders;

public static class ViewShaderMath
{
    // https://www.shadertoy.com/view/fsdyzB
    public static float SdRoundedBox(Vector2 p, Vector2 halfSize, Vector4 radius)
    {
        var cornerRadii = p.X > 0f ? radius.xy : radius.zw;
        var cornerRadius = p.Y > 0f ? cornerRadii.X : cornerRadii.Y;

        var q = Shader.Math.Abs(p) - halfSize + new Vector2(cornerRadius);
        return Shader.Math.Min(Shader.Math.Max(q.X, q.Y), 0f) +
               Shader.Math.Length(Shader.Math.Max(q, new Vector2(0f))) - cornerRadius;
    }

    public static Vector2 TransformPoint(Vector2 point, Matrix4x4 matrix)
    {
        return Vector4.Transform(new Vector4(point, 0f, 1f), matrix).xy;
    }

    public static float Median(float r, float g, float b)
    {
        return Shader.Math.Max(Shader.Math.Min(r, g), Shader.Math.Min(Shader.Math.Max(r, g), b));
    }

    public static float ScreenPxRange(Vector2 uv, Vector2 size, float pixelRange)
    {
        var unitRange = new Vector2(pixelRange) / size;
        var screenTexSize = new Vector2(1f) / Shader.Fwidth(uv);
        return Shader.Math.Max(0.5f * Shader.Math.Dot(unitRange, screenTexSize), 1f);
    }

    public static Vector3 Hsv2Rgb(Vector3 color)
    {
        var k = new Vector4(1f, 2f / 3f, 1f / 3f, 3f);
        var p = Shader.Math.Abs(Shader.Math.Frac(new Vector3(color.X) + k.xyz) * 6f - new Vector3(k.W));
        return color.Z * Shader.Math.Lerp(new Vector3(k.X), Shader.Math.Clamp(p - new Vector3(k.X), new Vector3(0f), new Vector3(1f)), color.Y);
    }

    /// <summary>
    ///     Fades <paramref name="color" /> to transparent outside a rounded rectangle of <paramref name="size" />
    ///     placed by the inverse of its transform.
    /// </summary>
    public static Vector4 ApplyBorderRadius(Vector2 fragPosition, Vector4 color, Vector4 radius, Vector2 size,
        Matrix4x4 inverseTransform, float transition)
    {
        var transformedFrag = Vector4.Transform(new Vector4(fragPosition, 0f, 1f), inverseTransform).xy;

        var halfSize = size / 2f;
        var distance = SdRoundedBox(transformedFrag - halfSize, halfSize, radius);
        var smoothedAlpha = Shader.Math.SmoothStep(0f, transition, distance);

        return Shader.Math.Lerp(color, new Vector4(color.xyz, 0f), smoothedAlpha);
    }
}
