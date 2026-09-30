using System.Numerics;

namespace Rin.Shade;

/// <summary>
///     Matrix functions a shader can call: plain C# that is both the CPU implementation and, through
///     Rin.Shade, the GPU one, so the two can't drift.
/// </summary>
[ShadeExport]
public static class MatrixMath
{
    /// <summary>
    ///     The inverse of a general 4x4 matrix, by cofactor expansion (the same algorithm as
    ///     <see cref="Matrix4x4.Invert(Matrix4x4, out Matrix4x4)" />, without its singular-matrix check).
    /// </summary>
    public static Matrix4x4 Inverse(Matrix4x4 matrix)
    {
        var row0 = matrix.Row(0);
        var row1 = matrix.Row(1);
        var row2 = matrix.Row(2);
        var row3 = matrix.Row(3);

        var a = row0.X;
        var b = row0.Y;
        var c = row0.Z;
        var d = row0.W;
        var e = row1.X;
        var f = row1.Y;
        var g = row1.Z;
        var h = row1.W;
        var i = row2.X;
        var j = row2.Y;
        var k = row2.Z;
        var l = row2.W;
        var m = row3.X;
        var n = row3.Y;
        var o = row3.Z;
        var p = row3.W;

        var kpLo = k * p - l * o;
        var jpLn = j * p - l * n;
        var joKn = j * o - k * n;
        var ipLm = i * p - l * m;
        var ioKm = i * o - k * m;
        var inJm = i * n - j * m;

        var a11 = f * kpLo - g * jpLn + h * joKn;
        var a12 = -(e * kpLo - g * ipLm + h * ioKm);
        var a13 = e * jpLn - f * ipLm + h * inJm;
        var a14 = -(e * joKn - f * ioKm + g * inJm);

        var inverseDeterminant = 1f / (a * a11 + b * a12 + c * a13 + d * a14);

        var gpHo = g * p - h * o;
        var fpHn = f * p - h * n;
        var foGn = f * o - g * n;
        var epHm = e * p - h * m;
        var eoGm = e * o - g * m;
        var enFm = e * n - f * m;

        var glHk = g * l - h * k;
        var flHj = f * l - h * j;
        var fkGj = f * k - g * j;
        var elHi = e * l - h * i;
        var ekGi = e * k - g * i;
        var ejFi = e * j - f * i;

        return new Matrix4x4(
            a11 * inverseDeterminant,
            -(b * kpLo - c * jpLn + d * joKn) * inverseDeterminant,
            (b * gpHo - c * fpHn + d * foGn) * inverseDeterminant,
            -(b * glHk - c * flHj + d * fkGj) * inverseDeterminant,
            a12 * inverseDeterminant,
            (a * kpLo - c * ipLm + d * ioKm) * inverseDeterminant,
            -(a * gpHo - c * epHm + d * eoGm) * inverseDeterminant,
            (a * glHk - c * elHi + d * ekGi) * inverseDeterminant,
            a13 * inverseDeterminant,
            -(a * jpLn - b * ipLm + d * inJm) * inverseDeterminant,
            (a * fpHn - b * epHm + d * enFm) * inverseDeterminant,
            -(a * flHj - b * elHi + d * ejFi) * inverseDeterminant,
            a14 * inverseDeterminant,
            (a * joKn - b * ioKm + c * inJm) * inverseDeterminant,
            -(a * foGn - b * eoGm + c * enFm) * inverseDeterminant,
            (a * fkGj - b * ekGi + c * ejFi) * inverseDeterminant);
    }
}
