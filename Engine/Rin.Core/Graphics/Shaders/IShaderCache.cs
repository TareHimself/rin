using System.Text.Json.Nodes;
using Rin.Core.Shared.Buffers;

namespace Rin.Core.Graphics.Shaders;

public interface IShaderCache
{
    public Task Put(string id, JsonObject data, Pair<ShaderStage, Buffer<byte>>[] stages);
    public bool Get(string id, out JsonObject data, out Pair<ShaderStage, Buffer<byte>>[] stages);
    public void Invalidate(string id);
}