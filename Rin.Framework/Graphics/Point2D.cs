using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Rin.Framework.Graphics;

[JsonConverter(typeof(JsonConverter))]
public record struct Point2D : IFormattable
{
    [PublicAPI] public int X = 0;

    [PublicAPI] public int Y = 0;

    public Point2D()
    {
    }

    public Point2D(int x, int y)
    {
        X = x;
        Y = y;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

        return
            $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}>";
    }


    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    public class JsonConverter : JsonConverter<Point2D>
    {
        public override void Write(Utf8JsonWriter writer, Point2D value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.X);
            writer.WriteNumber("Y", value.Y);
            writer.WriteEndObject();
        }

        public override Point2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var obj = JsonNode.Parse(ref reader)?.AsObject();
            Debug.Assert(obj is not null);
            return new Point2D(
                obj[nameof(X)]?.GetValue<int>() ?? 0,
                obj[nameof(Y)]?.GetValue<int>() ?? 0
            );
        }
    }
}