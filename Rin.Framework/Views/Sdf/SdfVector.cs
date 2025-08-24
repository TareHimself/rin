using System.Numerics;
using System.Text.Json.Serialization;
using Rin.Framework.Json.Converters;

namespace Rin.Framework.Views.Sdf;

public class SdfVector //: IJsonSerializable
{
    public string ImageId { get; set; } = string.Empty;

    [JsonConverter(typeof(Vector4JsonConverter))]
    public Vector4 Coordinates { get; set; } = new(0.0f, 0.0f, 1.0f, 1.0f);

    public string Id { get; set; } = string.Empty;

    public float PixelRange { get; set; }

    [JsonConverter(typeof(Vector2JsonConverter))]
    public Vector2 Offset { get; set; }

    [JsonConverter(typeof(Vector2JsonConverter))]
    public Vector2 Size { get; set; }

    // public void JsonSerialize(JsonObject output)
    // {
    //     output[nameof(Id)] = Id;
    //     output[nameof(AtlasIdx)] = AtlasIdx;
    //     output[nameof(Coordinates)] = Coordinates.ToJson();
    //     output[nameof(PixelRange)] = PixelRange;
    //     output[nameof(Offset)] = Offset.ToJson();
    //     output[nameof(Size)] = Size.ToJson();
    // }
    //
    // public void JsonDeserialize(JsonObject input)
    // {
    //     Id = input[nameof(Id)]?.GetValue<string>() ?? Id;
    //     AtlasIdx = input[nameof(AtlasIdx)]?.GetValue<int>() ?? AtlasIdx;
    //     Coordinates = input[nameof(Coordinates)]?.AsObject().ToVector4() ?? Coordinates;
    //     PixelRange = input[nameof(PixelRange)]?.GetValue<float>() ?? PixelRange;
    //     Offset = input[nameof(Offset)]?.AsObject().ToVector2() ?? Offset;
    //     Size = input[nameof(Size)]?.AsObject().ToVector2() ?? Size;
    // }
    //
    // class JsonConverter : JsonConverter<SdfVector>
    // {
    //     public override SdfVector? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //     {
    //         var input = JsonSerializer.Deserialize<JsonObject>(ref reader, options);
    //         
    //         if(input == null) return null;
    //         
    //         var result = new SdfVector();
    //         
    //         result.Id = input[nameof(Id)]?.GetValue<string>() ?? result.Id;
    //         result.AtlasIdx = input[nameof(AtlasIdx)]?.GetValue<int>() ?? result.AtlasIdx;
    //         result.Coordinates = input[nameof(Coordinates)]?.AsObject().ToVector4() ?? result.Coordinates;
    //         result.PixelRange = input[nameof(PixelRange)]?.GetValue<float>() ?? result.PixelRange;
    //         result.Offset = input[nameof(Offset)]?.AsObject().ToVector2() ?? result.Offset;
    //         result.Size = input[nameof(Size)]?.AsObject().ToVector2() ?? result.Size;
    //         return result;
    //     }
    //
    //     public override void Write(Utf8JsonWriter writer, SdfVector value, JsonSerializerOptions options)
    //     {
    //         writer.WriteStartObject();
    //         writer.WriteString(nameof(Id), value.Id);
    //         writer.WriteNumber(nameof(AtlasIdx), value.AtlasIdx);
    //         writer.WriteRawValue();
    //         foreach (var property in properties)
    //         {
    //             // write property name
    //             writer.WritePropertyName(property.Name);
    //             // let the serializer serialize the value itself
    //             // (so this converter will work with any other type, not just int)
    //             serializer.Serialize(writer, property.GetValue(value, null));
    //         }
    //
    //         writer.WriteEndObject();
    //     }
    // }
}