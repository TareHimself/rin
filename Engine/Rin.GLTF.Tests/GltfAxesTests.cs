using System.Numerics;
using Rin.Core.Shared.Math;

namespace Rin.GLTF.Tests;

public class GltfAxesTests
{
    private const float Tolerance = 1e-5f;

    [Test]
    public void AModelFacingGltfForwardFacesRinForward()
    {
        Assert.That(GltfAxes.ToRin(Vector3.UnitZ), Is.EqualTo(MathR.Forward));
        Assert.That(GltfAxes.ToRin(Vector3.UnitY), Is.EqualTo(MathR.Up));
    }

    [Test]
    public void RotatingThenConvertingMatchesConvertingThenRotating()
    {
        var rotation = Quaternion.Normalize(Quaternion.CreateFromYawPitchRoll(0.7f, -0.4f, 1.1f));
        var point = new Vector3(1f, 2f, 3f);

        var expected = GltfAxes.ToRin(Vector3.Transform(point, rotation));
        var actual = Vector3.Transform(GltfAxes.ToRin(point), GltfAxes.ToRin(rotation));

        Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Tolerance));
    }

    [Test]
    public void TransformingThenConvertingMatchesConvertingThenTransforming()
    {
        var matrix = Matrix4x4.CreateScale(2f, 1f, 0.5f)
                     * Matrix4x4.CreateFromYawPitchRoll(0.3f, 0.9f, -0.2f)
                     * Matrix4x4.CreateTranslation(4f, -1f, 6f);
        var point = new Vector3(-2f, 5f, 1f);

        var expected = GltfAxes.ToRin(Vector3.Transform(point, matrix));
        var actual = Vector3.Transform(GltfAxes.ToRin(point), GltfAxes.ToRin(matrix));

        Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Tolerance));
    }

    [Test]
    public void AReversedTriangleKeepsFacingItsConvertedNormal()
    {
        Vector3[] corners = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY];
        uint[] triangle = [0, 1, 2];
        var gltfNormal = FaceNormal(corners, triangle);

        var converted = corners.Select(GltfAxes.ToRin).ToArray();
        GltfAxes.ReverseWinding(triangle);

        Assert.That(FaceNormal(converted, triangle), Is.EqualTo(GltfAxes.ToRin(gltfNormal)));
    }

    private static Vector3 FaceNormal(Vector3[] corners, uint[] triangle)
    {
        var a = corners[triangle[0]];
        return Vector3.Cross(corners[triangle[1]] - a, corners[triangle[2]] - a);
    }
}
