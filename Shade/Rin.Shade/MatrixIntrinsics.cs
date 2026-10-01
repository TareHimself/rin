using System.Numerics;

namespace Rin.Shade;

/// <summary>
/// Row and column access for <see cref="Matrix4x4" />. Storage is row-major on both sides, so
/// <c>Row(i)</c> is the i-th row as it sits in memory and <c>Column(i)</c> gathers the i-th element
/// of every row. Each has a C# body so the same code also runs on the CPU.
/// </summary>
public static class MatrixIntrinsics
{
    /// <summary>
    /// Returns the row at <paramref name="index" />.
    /// </summary>
    [SlangExpression("@0[@1]")]
    public static Vector4 Row(this Matrix4x4 matrix, int index) => index switch
    {
        0 => new Vector4(matrix.M11, matrix.M12, matrix.M13, matrix.M14),
        1 => new Vector4(matrix.M21, matrix.M22, matrix.M23, matrix.M24),
        2 => new Vector4(matrix.M31, matrix.M32, matrix.M33, matrix.M34),
        _ => new Vector4(matrix.M41, matrix.M42, matrix.M43, matrix.M44)
    };

    /// <summary>
    /// Returns the column at <paramref name="index" />.
    /// </summary>
    [SlangExpression("transpose(@0)[@1]")]
    public static Vector4 Column(this Matrix4x4 matrix, int index) => index switch
    {
        0 => new Vector4(matrix.M11, matrix.M21, matrix.M31, matrix.M41),
        1 => new Vector4(matrix.M12, matrix.M22, matrix.M32, matrix.M42),
        2 => new Vector4(matrix.M13, matrix.M23, matrix.M33, matrix.M43),
        _ => new Vector4(matrix.M14, matrix.M24, matrix.M34, matrix.M44)
    };
}
