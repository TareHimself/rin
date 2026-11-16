using System.Numerics;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Tests.Graphics.ImageBasedLighting;

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
        var items = binner.Clusters.Items;
        var depths = new float[items.Length];
        for (var i = 0; i < depths.Length; i++) depths[i] = items[i].Center.Z;
        return depths;
    }

    private static ReadOnlySpan<uint> PointLightBins(LightBinner binner)
    {
        return binner.Clusters.BinsOf(ClusterItemType.PointLight);
    }

    [Test]
    public void BinOf_maps_near_to_the_first_bin_and_far_to_the_last()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ClusterBinner.BinOf(Near, Near, Far), Is.EqualTo(0));
            Assert.That(ClusterBinner.BinOf(Near * 0.5f, Near, Far), Is.EqualTo(0));
            Assert.That(ClusterBinner.BinOf(Far, Near, Far), Is.EqualTo(ClusterBinner.BinCount - 1));
            Assert.That(ClusterBinner.BinOf(Far * 10f, Near, Far), Is.EqualTo(ClusterBinner.BinCount - 1));
        });
    }

    [Test]
    public void BinOf_never_decreases_with_depth()
    {
        var previous = 0;
        for (var depth = Near; depth < Far; depth *= 1.01f)
        {
            var bin = ClusterBinner.BinOf(depth, Near, Far);
            Assert.That(bin, Is.GreaterThanOrEqualTo(previous), $"depth {depth}");
            previous = bin;
        }
    }

    [Test]
    public void Point_lights_are_sorted_nearest_first()
    {
        var binner = new LightBinner();

        binner.Bin([Point(30f, 1f), Point(10f, 1f), Point(20f, 1f)], [], Matrix4x4.Identity, Near, Far);

        Assert.That(Depths(binner), Is.EqualTo(new[] { 10f, 20f, 30f }));
    }

    [Test]
    public void Depth_is_measured_in_view_space()
    {
        var binner = new LightBinner();
        var cameraAtZ50 = Matrix4x4.CreateTranslation(0f, 0f, -50f);

        binner.Bin([Point(40f, 1f), Point(60f, 1f), Point(55f, 1f)], [], cameraAtZ50, Near, Far);

        Assert.That(Depths(binner), Is.EqualTo(new[] { 55f, 60f }), "the light behind the camera is dropped");
    }

    [Test]
    public void A_light_covers_exactly_the_bins_its_depth_range_touches()
    {
        var binner = new LightBinner();

        binner.Bin([Point(20f, 5f)], [], Matrix4x4.Identity, Near, Far);

        var first = ClusterBinner.BinOf(15f, Near, Far);
        var last = ClusterBinner.BinOf(25f, Near, Far);
        for (var bin = 0; bin < ClusterBinner.BinCount; bin++)
        {
            var covered = ClusterBinner.TryGetRange(PointLightBins(binner)[bin], out var lo, out var hi);
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

        binner.Bin(lights, [], Matrix4x4.Identity, Near, Far);

        var sorted = binner.Clusters.Items;
        for (var i = 0; i < sorted.Length; i++)
        {
            var first = ClusterBinner.BinOf(sorted[i].Center.Z - sorted[i].Radius, Near, Far);
            var last = ClusterBinner.BinOf(sorted[i].Center.Z + sorted[i].Radius, Near, Far);
            for (var bin = first; bin <= last; bin++)
            {
                Assert.That(ClusterBinner.TryGetRange(PointLightBins(binner)[bin], out var lo, out var hi), $"bin {bin}");
                Assert.That(i, Is.InRange(lo, hi), $"light {i} in bin {bin}");
            }
        }
    }

    [Test]
    public void Lights_out_of_range_are_dropped_and_directional_lights_are_kept_apart()
    {
        var binner = new LightBinner();

        binner.Bin([Point(-5f, 1f), Point(Far + 5f, 1f), Point(Far + 0.5f, 1f), Directional()], [], Matrix4x4.Identity,
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
        binner.Bin([Point(20f, 1f)], [], Matrix4x4.Identity, Near, Far);

        binner.Bin([Directional()], [], Matrix4x4.Identity, Near, Far);

        foreach (var bin in binner.Clusters.Bins) Assert.That(ClusterBinner.TryGetRange(bin, out _, out _), Is.False);
    }

    [Test]
    public void Box_volumes_are_binned_by_their_bounding_sphere_and_unbounded_ones_are_left_out()
    {
        var binner = new LightBinner();
        var box = TestEnvironments.Box(1, Vector3.One, new Vector3(0f, 0f, 20f), 3f);
        var sky = TestEnvironments.Unbounded(2, Vector3.One);

        binner.Bin([Point(10f, 1f)], [sky, box], Matrix4x4.Identity, Near, Far);

        var clusters = binner.Clusters;
        var volume = clusters.Items[clusters.StartOf(ClusterItemType.EnvironmentVolume)];
        Assert.Multiple(() =>
        {
            Assert.That(clusters.CountOf(ClusterItemType.PointLight), Is.EqualTo(1));
            Assert.That(clusters.CountOf(ClusterItemType.EnvironmentVolume), Is.EqualTo(1));
            Assert.That(volume.Index, Is.EqualTo(1u), "the box's position in the span it was given in");
            Assert.That(volume.Center, Is.EqualTo(new Vector3(0f, 0f, 20f)));
            Assert.That(volume.Radius, Is.EqualTo(box.BoundingRadius));
        });
    }

    [Test]
    public void Binning_the_same_number_of_lights_again_does_not_allocate()
    {
        var binner = new LightBinner();
        LightInfo[] lights = [Point(30f, 2f), Point(10f, 2f), Directional(), Point(20f, 2f)];
        binner.Bin(lights, [], Matrix4x4.Identity, Near, Far);

        var before = GC.GetAllocatedBytesForCurrentThread();
        binner.Bin(lights, [], Matrix4x4.Identity, Near, Far);

        Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
    }
}
