using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Mtsdf;

public class MtsdfAtlasGlyph
{
    public int Id;
    public int AtlasIdx;
    public Vector4<float> Coordinates = new(0.0f, 0.0f, 1.0f, 1.0f);
}