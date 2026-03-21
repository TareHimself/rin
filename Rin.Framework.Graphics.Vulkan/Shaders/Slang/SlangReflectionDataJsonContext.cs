using System.Text.Json.Serialization;

namespace Rin.Framework.Graphics.Vulkan.Shaders.Slang;

[JsonSerializable(typeof(SlangReflectionData))]
internal partial class SlangReflectionDataJsonContext : JsonSerializerContext
{
}