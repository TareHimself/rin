using System.Text.Json.Serialization;

namespace Rin.Slang;

[JsonSerializable(typeof(SlangReflectionData))]
public partial class SlangReflectionDataJsonContext : JsonSerializerContext
{
}
