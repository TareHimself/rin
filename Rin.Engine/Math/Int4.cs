using System.Numerics;

namespace Rin.Engine.Math;

/// <summary>
/// Should only be used for storage of 4 int components
/// </summary>
public record struct Int4
{
    public int X;
    public int Y;
    public int Z;
    public int W;

    public Int4()
    {
        
    }

    public Int4(int x, int y, int z, int w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }
    
    public static explicit operator Vector4(Int4 d) => new Vector4(d.X, d.Y, d.Z, d.W);
}