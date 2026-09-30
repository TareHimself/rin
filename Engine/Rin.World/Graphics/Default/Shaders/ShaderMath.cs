using System.Numerics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

public static class ShaderMath
{
    /// <summary>
    ///     The matrix-times-column-vector product the hand-written shaders use (<c>operators.slang</c>'s
    ///     <c>operator*(float4x4, float4)</c>). Vector4.Transform is the row-vector form, which is the
    ///     transpose of this, so skinning keeps this one until the CPU-side matrices are reconciled.
    /// </summary>
    [SlangCall("mul($0, $1)")]
    public static extern Vector4 MulColumn(Matrix4x4 matrix, Vector4 vector);

    /// <summary>
    ///     Transforms a normal without an inverse: rescales by the reciprocal squared axis lengths, then
    ///     applies the rotation-scale part of the matrix. https://lxjk.github.io/2017/10/01/Stop-Using-Normal-Matrix.html
    /// </summary>
    public static Vector3 TransformNormal(Vector3 normal, Matrix4x4 transformation)
    {
        var x = transformation.Column(0).xyz;
        var y = transformation.Column(1).xyz;
        var z = transformation.Column(2).xyz;

        var scaleSquared = new Vector3(MathIntrinsics.Dot(x, x), MathIntrinsics.Dot(y, y), MathIntrinsics.Dot(z, z));
        var scaledNormal = normal / MathIntrinsics.Sqrt(scaleSquared);
        var transformed = x * scaledNormal.X + y * scaledNormal.Y + z * scaledNormal.Z;

        return MathIntrinsics.Normalize(transformed);
    }
}
