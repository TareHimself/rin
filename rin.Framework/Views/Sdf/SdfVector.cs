using rin.Framework.Core.Math;

namespace rin.Framework.Views.Sdf;

public class SdfVector
{
    public string Id = "";
    public int AtlasIdx;
    public Vec4<float> Rect = new Vec4<float>(0.0f);
    public Vec4<float> Coordinates = new(0.0f, 0.0f, 1.0f, 1.0f);
}