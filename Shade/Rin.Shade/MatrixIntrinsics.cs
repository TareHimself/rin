using System.Numerics;

namespace Rin.Shade;

/// <summary>
/// Row and column access for Matrix4x4. The storage is row-major on both sides, so Row(i) is the
/// i-th row as it sits in memory and Column(i) gathers the i-th element of every row.
/// </summary>
public static class MatrixIntrinsics
{
    [SlangCall("$0[$1]")]
    public static extern Vector4 Row(this Matrix4x4 matrix, int index);

    [SlangCall("transpose($0)[$1]")]
    public static extern Vector4 Column(this Matrix4x4 matrix, int index);
}
