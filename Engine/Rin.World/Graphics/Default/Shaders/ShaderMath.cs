using System.Numerics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

[ShadeExport]
public static class ShaderMath
{
    /// <summary>
    ///     Transforms a normal without an inverse: rescales by the reciprocal squared axis lengths, then
    ///     applies the rotation-scale part of the matrix. https://lxjk.github.io/2017/10/01/Stop-Using-Normal-Matrix.html
    /// </summary>
    public static Vector3 TransformNormal(Vector3 normal, Matrix4x4 transformation)
    {
        var x = transformation.Row(0).xyz;
        var y = transformation.Row(1).xyz;
        var z = transformation.Row(2).xyz;

        var scaleSquared = new Vector3(Shader.Math.Dot(x, x), Shader.Math.Dot(y, y), Shader.Math.Dot(z, z));
        var scaledNormal = normal / Shader.Math.Sqrt(scaleSquared);
        var transformed = x * scaledNormal.X + y * scaledNormal.Y + z * scaledNormal.Z;

        return Shader.Math.Normalize(transformed);
    }
}
