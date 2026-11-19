using System.Numerics;

namespace Rin.GLTF;

/// <summary>
///     Converts glTF's right-handed space to Rin's left-handed one by mirroring X, so a model facing glTF's +Z faces
///     Rin's forward. glTF cameras and lights look down -Z, so they also need a half turn about Y.
/// </summary>
internal static class GltfAxes
{
    private static readonly Matrix4x4 Mirror = Matrix4x4.CreateScale(-1f, 1f, 1f);

    public static Vector3 ToRin(Vector3 vector) => new(-vector.X, vector.Y, vector.Z);

    public static Quaternion ToRin(Quaternion rotation) => new(rotation.X, -rotation.Y, -rotation.Z, rotation.W);

    public static Matrix4x4 ToRin(Matrix4x4 matrix) => Mirror * matrix * Mirror;

    // Mirroring flips every triangle's facing, so swapping two corners keeps it facing out.
    public static void ReverseWinding(Span<uint> triangles)
    {
        for (var i = 0; i + 2 < triangles.Length; i += 3)
            (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
    }
}
