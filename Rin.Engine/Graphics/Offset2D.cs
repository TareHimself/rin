using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Rin.Engine.Math;

namespace Rin.Engine.Graphics;

[JsonConverter(typeof(Offset2D.JsonConverter))]
public struct Offset2D : IEquatable<Offset2D>, IFormattable
{
    public bool Equals(Offset2D other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Offset2D other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

        return
            $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}>";
    }

    [PublicAPI] public uint X = 0;
    [PublicAPI] public uint Y = 0;

    public Offset2D()
    {
    }

    public Offset2D(uint x, uint y)
    {
        X = x;
        Y = y;
    }

    public Offset2D(int x, int y)
    {
        X = (uint)x;
        Y = (uint)y;
    }

    public static Offset2D Zero => new(0, 0);

    public void Deconstruct(out uint x, out uint y)
    {
        x = X;
        y = Y;
    }

    public class JsonConverter : JsonConverter<Extent2D>
    {
        public override Extent2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var obj = JsonNode.Parse(ref reader)?.AsObject();
            Debug.Assert(obj is not null);
            return new Extent2D(
                obj[nameof(Extent2D.Width)]?.GetValue<uint>() ?? 0,
                obj[nameof(Extent2D.Height)]?.GetValue<uint>() ?? 0
            );
        }

        public override void Write(Utf8JsonWriter writer, Extent2D value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Width", value.Width);
            writer.WriteNumber("Height", value.Height);
            writer.WriteEndObject();
        }
    }
}