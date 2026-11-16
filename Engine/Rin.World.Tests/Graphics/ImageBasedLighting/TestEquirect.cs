using System.Numerics;
using Rin.Core.Graphics;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Tests.Graphics.ImageBasedLighting;

/// <summary>
///     Builds float RGBA panoramas by asking a function for the colour seen in each direction.
/// </summary>
internal static class TestEquirect
{
    public static readonly Extent2D Extent = new(128u, 64u);

    public static float[] Create(Func<Vector3, Vector3> colourOf)
    {
        var rgba = new float[Extent.Width * Extent.Height * 4];
        for (var y = 0; y < Extent.Height; y++)
        for (var x = 0; x < Extent.Width; x++)
        {
            var colour = colourOf(Panorama.Direction((x + 0.5f) / Extent.Width, (y + 0.5f) / Extent.Height));
            new Vector4(colour, 1f).CopyTo(rgba.AsSpan((int)(y * Extent.Width + x) * 4, 4));
        }

        return rgba;
    }

    public static float[] Constant(Vector3 colour)
    {
        return Create(_ => colour);
    }
}
