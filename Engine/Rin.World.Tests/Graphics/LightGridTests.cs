using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Tests.Graphics;

public class LightGridTests
{
    private const uint Width = 1920;
    private const uint Height = 1080;
    private const float FieldOfView = 70f;
    private static readonly float TanHalfFovY = float.Tan(float.DegreesToRadians(FieldOfView / 2f));
    private const float Aspect = Width / (float)Height;

    private static Vector4 Slopes(uint tileX, uint tileY) =>
        LightGrid.TileSlopes(tileX, tileY, Width, Height, TanHalfFovY, Aspect);

    [Test]
    public void One_tile_covering_the_screen_spans_the_field_of_view()
    {
        var slopes = LightGrid.TileSlopes(0, 0, 32, 32, 1f, 1f);

        Assert.That(slopes, Is.EqualTo(new Vector4(-1f, 1f, -1f, 1f)));
    }

    [Test]
    public void The_last_tile_stops_at_the_screen_edge()
    {
        var lastX = (Width - 1) / LightGrid.TileSize;
        var lastY = (Height - 1) / LightGrid.TileSize;

        var slopes = Slopes(lastX, lastY);

        Assert.Multiple(() =>
        {
            Assert.That(slopes.Y, Is.EqualTo(TanHalfFovY * Aspect).Within(1e-5f), "right edge");
            Assert.That(slopes.Z, Is.EqualTo(-TanHalfFovY).Within(1e-5f), "bottom edge");
        });
    }

    [Test]
    public void A_projected_point_lands_in_the_tile_whose_slopes_contain_it()
    {
        var projection = MathR.PerspectiveProjection(FieldOfView, Width, Height, 0.1f);
        var random = new Random(3);

        for (var i = 0; i < 500; i++)
        {
            var z = 0.5f + random.NextSingle() * 50f;
            var point = new Vector3(
                (random.NextSingle() * 2f - 1f) * TanHalfFovY * Aspect * z * 0.99f,
                (random.NextSingle() * 2f - 1f) * TanHalfFovY * z * 0.99f,
                z);

            var clip = Vector4.Transform(new Vector4(point, 1f), projection);
            var pixelX = (clip.X / clip.W * 0.5f + 0.5f) * Width;
            var pixelY = (clip.Y / clip.W * 0.5f + 0.5f) * Height;
            var slopes = Slopes((uint)pixelX / LightGrid.TileSize, (uint)pixelY / LightGrid.TileSize);

            Assert.That(point.X / z, Is.InRange(slopes.X - 1e-4f, slopes.Y + 1e-4f), $"x of {point}");
            Assert.That(point.Y / z, Is.InRange(slopes.Z - 1e-4f, slopes.W + 1e-4f), $"y of {point}");
        }
    }

    [Test]
    public void A_sphere_touches_tiles_it_overlaps_and_not_tiles_it_misses()
    {
        var slopes = new Vector4(-0.1f, 0.1f, -0.1f, 0.1f);

        Assert.Multiple(() =>
        {
            Assert.That(LightGrid.SphereTouchesTile(new Vector3(0f, 0f, 10f), 0.1f, slopes), "centred");
            Assert.That(LightGrid.SphereTouchesTile(new Vector3(1.5f, 0f, 10f), 0.6f, slopes), "reaching in from the right");
            Assert.That(LightGrid.SphereTouchesTile(new Vector3(3f, 0f, 10f), 0.6f, slopes), Is.False, "right of the tile");
            Assert.That(LightGrid.SphereTouchesTile(new Vector3(-3f, 0f, 10f), 0.6f, slopes), Is.False, "left of the tile");
            Assert.That(LightGrid.SphereTouchesTile(new Vector3(0f, 3f, 10f), 0.6f, slopes), Is.False, "above the tile");
            Assert.That(LightGrid.SphereTouchesTile(new Vector3(0f, -3f, 10f), 0.6f, slopes), Is.False, "below the tile");
            Assert.That(LightGrid.SphereTouchesTile(new Vector3(5f, 0f, 1f), 10f, slopes), "big enough to cover it");
        });
    }

    [TestCase(0u, 0u, 31u, 0xFFFFFFFFu)]
    [TestCase(0u, 3u, 5u, 0b111000u)]
    [TestCase(1u, 3u, 40u, 0x1FFu)]
    [TestCase(1u, 40u, 70u, 0xFFFFFF00u)]
    [TestCase(2u, 10u, 64u, 1u)]
    public void RangeMask_keeps_only_the_bits_inside_the_range(uint word, uint first, uint last, uint expected)
    {
        Assert.That(LightGrid.RangeMask(word, first, last), Is.EqualTo(expected));
    }
}
