using System.Numerics;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;

namespace Rin.World.Tests.Graphics;

public class LightBinnerTests
{
    private const float Near = 0.1f;
    private const float Far = 100f;

    private static LightInfo Point(float depth, float radius) => new()
    {
        Location = new Vector3(0f, 0f, depth),
        Radius = radius,
        Direction = Vector3.Zero,
        Color = Vector3.One,
        Radiance = 1f,
        LightType = LightType.Point
    };

    private static LightInfo Directional() => Point(0f, 0f) with { LightType = LightType.Directional };

    private static float[] Depths(LightBinner binner)
    {
        var depths = new float[binner.PointLights.Length];
        for (var i = 0; i < depths.Length; i++) depths[i] = binner.PointLights[i].Location.Z;
        return depths;
    }

    [Test]
    public void BinOf_maps_near_to_the_first_bin_and_far_to_the_last()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LightBinner.BinOf(Near, Near, Far), Is.EqualTo(0));
            Assert.That(LightBinner.BinOf(Near * 0.5f, Near, Far), Is.EqualTo(0));
            Assert.That(LightBinner.BinOf(Far, Near, Far), Is.EqualTo(LightBinner.BinCount - 1));
            Assert.That(LightBinner.BinOf(Far * 10f, Near, Far), Is.EqualTo(LightBinner.BinCount - 1));
        });
    }

    [Test]
    public void BinOf_never_decreases_with_depth()
    {
        var previous = 0;
        for (var depth = Near; depth < Far; depth *= 1.01f)
        {
            var bin = LightBinner.BinOf(depth, Near, Far);
            Assert.That(bin, Is.GreaterThanOrEqualTo(previous), $"depth {depth}");
            previous = bin;
        }
    }

    [Test]
    public void Point_lights_are_sorted_nearest_first()
    {
        var binner = new LightBinner();

        binner.Bin([Point(30f, 1f), Point(10f, 1f), Point(20f, 1f)], Matrix4x4.Identity, Near, Far);

        Assert.That(Depths(binner), Is.EqualTo(new[] { 10f, 20f, 30f }));
    }

    [Test]
    public void Depth_is_measured_in_view_space()
    {
        var binner = new LightBinner();
        var cameraAtZ50 = Matrix4x4.CreateTranslation(0f, 0f, -50f);

        binner.Bin([Point(40f, 1f), Point(60f, 1f), Point(55f, 1f)], cameraAtZ50, Near, Far);

        Assert.That(Depths(binner), Is.EqualTo(new[] { 55f, 60f }), "the light behind the camera is dropped");
    }

    [Test]
    public void A_light_covers_exactly_the_bins_its_depth_range_touches()
    {
        var binner = new LightBinner();

        binner.Bin([Point(20f, 5f)], Matrix4x4.Identity, Near, Far);

        var first = LightBinner.BinOf(15f, Near, Far);
        var last = LightBinner.BinOf(25f, Near, Far);
        for (var bin = 0; bin < LightBinner.BinCount; bin++)
        {
            var covered = LightBinner.TryGetRange(binner.Bins[bin], out var lo, out var hi);
            if (bin >= first && bin <= last)
                Assert.That(covered && lo == 0 && hi == 0, $"bin {bin} should hold light 0");
            else
                Assert.That(covered, Is.False, $"bin {bin} should be empty");
        }
    }

    [Test]
    public void Every_light_overlapping_a_bin_is_inside_its_range()
    {
        var binner = new LightBinner();
        var lights = new LightInfo[200];
        var random = new Random(7);
        for (var i = 0; i < lights.Length; i++)
            lights[i] = Point(Near + random.NextSingle() * Far, random.NextSingle() * 10f);

        binner.Bin(lights, Matrix4x4.Identity, Near, Far);

        var sorted = binner.PointLights;
        for (var i = 0; i < sorted.Length; i++)
        {
            var first = LightBinner.BinOf(sorted[i].Location.Z - sorted[i].Radius, Near, Far);
            var last = LightBinner.BinOf(sorted[i].Location.Z + sorted[i].Radius, Near, Far);
            for (var bin = first; bin <= last; bin++)
            {
                Assert.That(LightBinner.TryGetRange(binner.Bins[bin], out var lo, out var hi), $"bin {bin}");
                Assert.That(i, Is.InRange(lo, hi), $"light {i} in bin {bin}");
            }
        }
    }

    [Test]
    public void Lights_out_of_range_are_dropped_and_directional_lights_are_kept_apart()
    {
        var binner = new LightBinner();

        binner.Bin([Point(-5f, 1f), Point(Far + 5f, 1f), Point(Far + 0.5f, 1f), Directional()], Matrix4x4.Identity,
            Near, Far);

        Assert.Multiple(() =>
        {
            Assert.That(Depths(binner), Is.EqualTo(new[] { Far + 0.5f }), "a light reaching back past far stays");
            Assert.That(binner.DirectionalLights.Length, Is.EqualTo(1));
        });
    }

    [Test]
    public void With_no_point_lights_every_bin_is_empty()
    {
        var binner = new LightBinner();
        binner.Bin([Point(20f, 1f)], Matrix4x4.Identity, Near, Far);

        binner.Bin([Directional()], Matrix4x4.Identity, Near, Far);

        foreach (var bin in binner.Bins) Assert.That(LightBinner.TryGetRange(bin, out _, out _), Is.False);
    }

    [Test]
    public void Binning_the_same_number_of_lights_again_does_not_allocate()
    {
        var binner = new LightBinner();
        LightInfo[] lights = [Point(30f, 2f), Point(10f, 2f), Directional(), Point(20f, 2f)];
        binner.Bin(lights, Matrix4x4.Identity, Near, Far);

        var before = GC.GetAllocatedBytesForCurrentThread();
        binner.Bin(lights, Matrix4x4.Identity, Near, Far);

        Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
    }
}
