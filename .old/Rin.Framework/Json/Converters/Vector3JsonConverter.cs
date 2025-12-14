using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Rin.Framework.Json.Converters;

public class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var obj = JsonNode.Parse(ref reader)?.AsObject();
        Debug.Assert(obj is not null);

        var x = obj[nameof(Vector4.X)]?.GetValue<float>() ?? 0;
        var y = obj[nameof(Vector4.Y)]?.GetValue<float>() ?? 0;
        var z = obj[nameof(Vector4.Z)]?.GetValue<float>() ?? 0;
        return new Vector3(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteNumber("Z", value.Z);
        writer.WriteEndObject();
    }
}