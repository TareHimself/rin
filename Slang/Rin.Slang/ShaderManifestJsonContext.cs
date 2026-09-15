using System.Text.Json.Serialization;

namespace Rin.Slang;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ShaderManifest))]
public partial class ShaderManifestJsonContext : JsonSerializerContext
{
}
