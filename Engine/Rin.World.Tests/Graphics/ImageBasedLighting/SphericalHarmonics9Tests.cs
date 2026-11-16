using System.Numerics;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Tests.Graphics.ImageBasedLighting;

public class SphericalHarmonics9Tests
{
    private const int Width = 128;
    private const int Height = 64;

    private static SphericalHarmonics9 Project(Func<Vector3, Vector3> radianceOf)
    {
        var projected = new SphericalHarmonics9();
        var texelAngles = MathF.Tau / Width * (MathF.PI / Height);
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            var v = (y + 0.5f) / Height;
            var direction = Panorama.Direction((x + 0.5f) / Width, v);
            projected.Add(direction, radianceOf(direction), texelAngles * MathF.Sin(v * MathF.PI));
        }

        return projected;
    }

    private static void AssertClose(Vector3 actual, Vector3 expected, float tolerance)
    {
        Assert.That(Vector3.Distance(actual, expected), Is.LessThan(tolerance), $"{actual} vs {expected}");
    }

    [Test]
    public void AConstantFunctionOnlyFillsTheFirstCoefficient()
    {
        var projected = Project(_ => new Vector3(1f, 2f, 3f));

        Assert.Multiple(() =>
        {
            AssertClose(projected[0], new Vector3(1f, 2f, 3f) * MathF.Sqrt(4f * MathF.PI), 0.01f);
            for (var i = 1; i < SphericalHarmonics9.CoefficientCount; i++)
                AssertClose(projected[i], Vector3.Zero, 0.01f);
        });
    }

    [Test]
    public void EvaluatingAProjectedConstantReturnsIt()
    {
        var projected = Project(_ => new Vector3(1f, 2f, 3f));

        AssertClose(projected.Evaluate(Vector3.Normalize(new Vector3(1f, -2f, 3f))), new Vector3(1f, 2f, 3f), 0.01f);
    }

    [Test]
    public void IrradianceUnderAConstantSkyIsItsRadianceTimesPi()
    {
        var irradiance = Project(_ => Vector3.One).ConvolvedWithCosineLobe();

        AssertClose(irradiance.Evaluate(Vector3.UnitX), new Vector3(MathF.PI), 0.01f);
    }

    [Test]
    public void IrradianceUnderALitUpperHemisphereFallsOffTowardsTheGround()
    {
        var irradiance = Project(direction => direction.Y > 0f ? Vector3.One : Vector3.Zero).ConvolvedWithCosineLobe();

        Assert.Multiple(() =>
        {
            AssertClose(irradiance.Evaluate(Vector3.UnitY), new Vector3(MathF.PI), 0.05f);
            AssertClose(irradiance.Evaluate(Vector3.UnitZ), new Vector3(MathF.PI / 2f), 0.05f);
            AssertClose(irradiance.Evaluate(-Vector3.UnitY), Vector3.Zero, 0.05f);
        });
    }

    [Test]
    public void KeepsTheDirectionOfAOneSidedLight()
    {
        var projected = Project(direction => direction.X > 0.9f ? Vector3.One : Vector3.Zero);

        Assert.That(projected.Evaluate(Vector3.UnitX).X, Is.GreaterThan(projected.Evaluate(-Vector3.UnitX).X));
    }
}
