using JetBrains.Annotations;

namespace rin.Framework.Graphics;

public struct Extent2D
{
    [PublicAPI]
    public uint Width = 0;
    [PublicAPI]
    public uint Height = 0;

    public Extent2D()
    {
    }
    
    public Extent2D(uint width, uint height) 
    {
        Width = width;
        Height = height;
    }
    
    
}