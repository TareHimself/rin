using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rin.World.Graphics.ImageBasedLighting;

/// <summary>
///     An RGB function over the sphere stored as 9 spherical-harmonic coefficients (bands 0 to 2), enough for
///     smoothly varying light such as the diffuse light arriving from an environment.
/// </summary>
public struct SphericalHarmonics9
{
    public const int CoefficientCount = 9;

    private const float CosineLobeBand0 = MathF.PI;
    private const float CosineLobeBand1 = MathF.PI * 2f / 3f;
    private const float CosineLobeBand2 = MathF.PI / 4f;

    private Coefficients _coefficients;

    public Vector3 this[int index]
    {
        readonly get => _coefficients[index];
        set => _coefficients[index] = value;
    }

    public void Add(Vector3 direction, Vector3 value, float solidAngle)
    {
        Span<float> basis = stackalloc float[CoefficientCount];
        EvaluateBasis(direction, basis);
        for (var i = 0; i < CoefficientCount; i++) _coefficients[i] += value * (basis[i] * solidAngle);
    }

    public readonly Vector3 Evaluate(Vector3 direction)
    {
        Span<float> basis = stackalloc float[CoefficientCount];
        EvaluateBasis(direction, basis);
        var result = Vector3.Zero;
        for (var i = 0; i < CoefficientCount; i++) result += _coefficients[i] * basis[i];
        return result;
    }

    /// <summary>
    ///     Turns projected radiance into irradiance: evaluating the result at a normal gives the cosine-weighted
    ///     light arriving over the hemisphere around it.
    /// </summary>
    public readonly SphericalHarmonics9 ConvolvedWithCosineLobe()
    {
        var result = this;
        result[0] *= CosineLobeBand0;
        for (var i = 1; i < 4; i++) result[i] *= CosineLobeBand1;
        for (var i = 4; i < CoefficientCount; i++) result[i] *= CosineLobeBand2;
        return result;
    }

    public readonly IrradianceCoefficients ToCoefficients()
    {
        return new IrradianceCoefficients
        {
            L00 = this[0],
            L1M1 = this[1],
            L10 = this[2],
            L11 = this[3],
            L2M2 = this[4],
            L2M1 = this[5],
            L20 = this[6],
            L21 = this[7],
            L22 = this[8]
        };
    }

    public readonly SphericalHarmonics9 Scaled(float scale)
    {
        var result = this;
        for (var i = 0; i < CoefficientCount; i++) result[i] *= scale;
        return result;
    }

    private static void EvaluateBasis(Vector3 direction, Span<float> basis)
    {
        var (x, y, z) = (direction.X, direction.Y, direction.Z);
        basis[0] = 0.282095f;
        basis[1] = 0.488603f * y;
        basis[2] = 0.488603f * z;
        basis[3] = 0.488603f * x;
        basis[4] = 1.092548f * x * y;
        basis[5] = 1.092548f * y * z;
        basis[6] = 0.315392f * (3f * z * z - 1f);
        basis[7] = 1.092548f * x * z;
        basis[8] = 0.546274f * (x * x - y * y);
    }

    [InlineArray(CoefficientCount)]
    private struct Coefficients
    {
        private Vector3 _element;
    }
}
