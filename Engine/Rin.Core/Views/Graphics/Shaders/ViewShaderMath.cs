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
