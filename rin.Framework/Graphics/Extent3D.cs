using JetBrains.Annotations;

namespace rin.Framework.Graphics;

public struct Extent3D
{
    [PublicAPI]
    public uint Width = 0;
    [PublicAPI]
    public uint Height = 0;
    [PublicAPI]
    public uint Dimensions = 1;

    public Extent3D()
    {
    }
    
    public Extent3D(uint width, uint height, uint dimensions = 1) 
    {
        Width = width;
        Height = height;
        Dimensions = dimensions;
    }

    
    public static implicit operator Extent2D(Extent3D extent) => new Extent2D(extent.Width, extent.Height);
}