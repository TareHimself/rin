using System.Numerics;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Tests.Graphics;

public class ShaderMathTests
{
    private const float Tolerance = 1e-4f;

    [Test]
    public void ANormalUnderNonUniformScaleMatchesTheInverseTranspose()
    {
        var transform = Matrix4x4.CreateScale(20f, 1f, 20f) * Matrix4x4.CreateRotationY(0.6f) *
                        Matrix4x4.CreateTranslation(3f, -1f, 2f);
        var normal = Vector3.Normalize(new Vector3(0f, 1f, 1f));
        Matrix4x4.Invert(transform, out var inverse);
        var expected = Vector3.Normalize(Vector3.TransformNormal(normal, Matrix4x4.Transpose(inverse)));

        var transformed = ShaderMath.TransformNormal(normal, transform);

        Assert.That(Vector3.Distance(transformed, expected), Is.LessThan(Tolerance));
    }

    [Test]
    public void AFlattenedSlopeFacesMostlyUp()
    {
        var normal = Vector3.Normalize(new Vector3(0f, 1f, 1f));

        var transformed = ShaderMath.TransformNormal(normal, Matrix4x4.CreateScale(20f, 1f, 20f));

        Assert.That(transformed.Y, Is.GreaterThan(0.99f));
    }
}
