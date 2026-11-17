using System.Numerics;

namespace Rin.Shade.Tests;

public class ShaderIntrinsicTests
{
    [TestCase(0b1011_0000u, 4u)]
    [TestCase(1u, 0u)]
    [TestCase(0x8000_0000u, 31u)]
    [TestCase(0u, 0xFFFF_FFFFu)]
    public void FirstBitLowIsTheIndexOfTheLowestSetBit(uint value, uint expected)
    {
        Assert.That(Shader.FirstBitLow(value), Is.EqualTo(expected));
    }

    [TestCase(0u, 0u)]
    [TestCase(0b1011_0000u, 3u)]
    [TestCase(0xFFFF_FFFFu, 32u)]
    public void CountBitsCountsSetBits(uint value, uint expected)
    {
        Assert.That(Shader.CountBits(value), Is.EqualTo(expected));
    }

    [Test]
    public void AsUIntKeepsTheBitsOfAFloat()
    {
        Assert.That(Shader.AsUInt(1f), Is.EqualTo(0x3F80_0000u));
    }

    [Test]
    public void SignOfNotANumberIsZero()
    {
        Assert.That(Shader.Sign(float.NaN), Is.EqualTo(0f));
    }

    [Test]
    public void ClampWithCrossedLimitsDoesNotThrow()
    {
        Assert.That(Shader.Clamp(0.5f, 1f, 0f), Is.EqualTo(0f));
    }

    [Test]
    public void RefractPastTheCriticalAngleIsZero()
    {
        var grazing = Vector3.Normalize(new Vector3(1f, -0.1f, 0f));

        Assert.That(Shader.Refract(grazing, Vector3.UnitY, 1.5f), Is.EqualTo(Vector3.Zero));
    }
}
