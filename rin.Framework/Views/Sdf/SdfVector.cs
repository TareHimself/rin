using System.Numerics;

namespace rin.Framework.Views.Sdf;

public class SdfVector
{
    public int AtlasIdx;

    public Vector4 Coordinates = new(0.0f, 0.0f, 1.0f, 1.0f);
    public string Id = "";
    public float Range { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}