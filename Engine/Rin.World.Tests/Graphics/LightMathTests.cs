using System.Numerics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Graphics.Surfaces;

namespace Rin.World.Tests.Graphics;

public class LightMathTests
{
    private static readonly Vector3 Gold = new(1f, 0.77f, 0.34f);

    [Test]
    public void ANonMetalMirrorsAFewPercentWhateverItsColour()
    {
        var surface = new Surface { BaseColor = Gold, Specular = 0.5f, Metallic = 0f };

        Assert.That(LightMath.BaseReflectivity(surface), Is.EqualTo(new Vector3(0.04f)));
    }

    [Test]
    public void AMetalMirrorsItsBaseColour()
    {
        var surface = new Surface { BaseColor = Gold, Specular = 0.5f, Metallic = 1f };

        Assert.That(Vector3.Distance(LightMath.BaseReflectivity(surface), Gold), Is.LessThan(1e-6f));
    }

    [Test]
    public void TheShaderAndTheBinnerPutADepthInTheSameBin()
    {
        const float near = 0.1f;
        const float far = 100f;
        var scale = ClusterBinner.BinScale(near, far);

        Assert.Multiple(() =>
        {
            for (var depth = near * 0.5f; depth < far * 2f; depth *= 1.07f)
                Assert.That((int)LightGrid.BinOf(depth, near, scale), Is.EqualTo(ClusterBinner.BinOf(depth, near, far)),
                    $"depth {depth}");
        });
    }
}
