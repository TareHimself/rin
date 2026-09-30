using System.Numerics;

namespace Rin.Shade;

/// <summary>
/// Row and column access for Matrix4x4. The storage is row-major on both sides, so Row(i) is the
/// i-th row as it sits in memory and Column(i) gathers the i-th element of every row. Each has a real
/// C# body so code written with it also runs on the CPU; the [SlangExpression] binding is what a shader uses.
/// </summary>
public static class MatrixIntrinsics
{
    [SlangExpression("@0[@1]")]
    public static Vector4 Row(this Matrix4x4 matrix, int index) => index switch
    {
        0 => new Vector4(matrix.M11, matrix.M12, matrix.M13, matrix.M14),
        1 => new Vector4(matrix.M21, matrix.M22, matrix.M23, matrix.M24),
        2 => new Vector4(matrix.M31, matrix.M32, matrix.M33, matrix.M34),
        _ => new Vector4(matrix.M41, matrix.M42, matrix.M43, matrix.M44)
    };

    [SlangExpression("transpose(@0)[@1]")]
    public static Vector4 Column(this Matrix4x4 matrix, int index) => index switch
    {
        0 => new Vector4(matrix.M11, matrix.M21, matrix.M31, matrix.M41),
        1 => new Vector4(matrix.M12, matrix.M22, matrix.M32, matrix.M42),
        2 => new Vector4(matrix.M13, matrix.M23, matrix.M33, matrix.M43),
        _ => new Vector4(matrix.M14, matrix.M24, matrix.M34, matrix.M44)
    };
}
