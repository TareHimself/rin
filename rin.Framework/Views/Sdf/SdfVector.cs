using System.Numerics;
using rin.Framework.Core.Math;

namespace rin.Framework.Views.Sdf;

public class SdfVector
{
    public string Id = "";
    
    public int AtlasIdx;
    public float Range { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    
    public Vector4 Coordinates = new(0.0f, 0.0f, 1.0f, 1.0f);
}