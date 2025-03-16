using System.Numerics;
using System.Text.Json.Nodes;
using Rin.Engine.Core;
using Rin.Engine.Core.Math;

namespace Rin.Engine.Views.Sdf;

public class SdfVector : IJsonSerializable
{
    public string Id = "";
    
    public int AtlasIdx;

    public Vector4 Coordinates = new(0.0f, 0.0f, 1.0f, 1.0f);
    
    public float PixelRange { get; set; }
    public Vector2 Offset { get; set; }
    public Vector2 Size { get; set; }
    public void JsonSerialize(JsonObject output)
    {
        output[nameof(Id)] = Id;
        output[nameof(AtlasIdx)] = AtlasIdx;
        output[nameof(Coordinates)] = Coordinates.ToJson();
        output[nameof(PixelRange)] = PixelRange;
        output[nameof(Offset)] = Offset.ToJson();
        output[nameof(Size)] = Size.ToJson();
    }

    public void JsonDeserialize(JsonObject input)
    {
        Id = input[nameof(Id)]?.GetValue<string>() ?? Id;
        AtlasIdx = input[nameof(AtlasIdx)]?.GetValue<int>() ?? AtlasIdx;
        Coordinates = input[nameof(Coordinates)]?.AsObject().ToVector4() ?? Coordinates;
        PixelRange =  input[nameof(PixelRange)]?.GetValue<float>() ?? PixelRange;
        Offset = input[nameof(Offset)]?.AsObject().ToVector2() ?? Offset;
        Size = input[nameof(Size)]?.AsObject().ToVector2() ?? Size;
    }
}