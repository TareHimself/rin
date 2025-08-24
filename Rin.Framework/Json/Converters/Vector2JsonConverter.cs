using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Rin.Framework.Json.Converters;

public class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var obj = JsonNode.Parse(ref reader)?.AsObject();
        Debug.Assert(obj is not null);
        var x = obj[nameof(Vector4.X)]?.GetValue<float>() ?? 0;
        var y = obj[nameof(Vector4.Y)]?.GetValue<float>() ?? 0;
        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteEndObject();
    }
}