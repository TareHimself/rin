using rin.Core.Math;

namespace rin.Widgets.Sdf;

public class SdfVector
{
    public string Id = "";
    public int AtlasIdx;
    public Vector4<float> Rect = new Vector4<float>(0.0f);
    public Vector4<float> Coordinates = new(0.0f, 0.0f, 1.0f, 1.0f);
}