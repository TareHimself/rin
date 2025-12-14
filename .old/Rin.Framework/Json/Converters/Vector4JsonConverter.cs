using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Rin.Framework.Json.Converters;

public class Vector4JsonConverter : JsonConverter<Vector4>
{
    public override Vector4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var obj = JsonNode.Parse(ref reader)?.AsObject();
        Debug.Assert(obj is not null);

        var x = obj[nameof(Vector4.X)]?.GetValue<float>() ?? 0;
        var y = obj[nameof(Vector4.Y)]?.GetValue<float>() ?? 0;
        var z = obj[nameof(Vector4.Z)]?.GetValue<float>() ?? 0;
        var w = obj[nameof(Vector4.W)]?.GetValue<float>() ?? 0;
        return new Vector4(x, y, z, w);
    }

    public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(nameof(Vector4.X), value.X);
        writer.WriteNumber(nameof(Vector4.Y), value.Y);
        writer.WriteNumber(nameof(Vector4.Z), value.Z);
        writer.WriteNumber(nameof(Vector4.W), value.W);
        writer.WriteEndObject();
    }
}