using System.Text.Json.Serialization;

namespace Rin.Slang;

[JsonConverter(typeof(JsonStringEnumConverter<ShaderKind>))]
public enum ShaderKind
{
    Graphics,
    Compute
}
