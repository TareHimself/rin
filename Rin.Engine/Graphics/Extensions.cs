using Rin.Engine.Graphics.Meshes;

namespace Rin.Engine.Graphics;

public static class Extensions
{
    public static uint ComponentCount(this ImageFormat format)
    {
        return format switch
        {
            ImageFormat.R8 => 1,
            ImageFormat.R16 => 1,
            ImageFormat.R32 => 1,
            ImageFormat.RG8 => 2,
            ImageFormat.RG16 => 2,
            ImageFormat.RG32 => 2,
            ImageFormat.RGB8 => 3,
            ImageFormat.RGB16 => 3,
            ImageFormat.RGB32 => 3,
            ImageFormat.RGBA8 => 4,
            ImageFormat.RGBA16 => 4,
            ImageFormat.RGBA32 => 4,
            ImageFormat.Depth => 1,
            ImageFormat.Stencil => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    public static nuint PixelByteSize(this ImageFormat format)
    {
        return format.ComponentCount() * (nuint)(format switch
        {
            ImageFormat.R8 => 1,
            ImageFormat.R16 => 2,
            ImageFormat.R32 => 4,
            ImageFormat.RG8 => 1,
            ImageFormat.RG16 => 2,
            ImageFormat.RG32 => 4,
            ImageFormat.RGB8 => 1,
            ImageFormat.RGB16 => 2,
            ImageFormat.RGB32 => 4,
            ImageFormat.RGBA8 => 1,
            ImageFormat.RGBA16 => 2,
            ImageFormat.RGBA32 => 4,
            ImageFormat.Depth => 4,
            ImageFormat.Stencil => 4, // Kinda finiky
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        });
    }

    public static Bounds3D ComputeBounds(this IEnumerable<Vertex> vertices)
    {
        var bounds = new Bounds3D();

        foreach (var vertex in vertices)
        {
            var location = vertex.Location;
            bounds.X.Max = float.Max(bounds.X.Max, location.X);
            bounds.X.Min = float.Min(bounds.X.Min, location.X);
            bounds.Y.Max = float.Max(bounds.Y.Max, location.Y);
            bounds.Y.Min = float.Min(bounds.Y.Min, location.Y);
            bounds.Z.Max = float.Max(bounds.Z.Max, location.Z);
            bounds.Z.Min = float.Min(bounds.Z.Min, location.Z);
        }

        return bounds;
    }
}