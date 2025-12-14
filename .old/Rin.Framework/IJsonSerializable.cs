using System.Text.Json.Nodes;

namespace Rin.Framework;

public interface IJsonSerializable
{
    public void JsonSerialize(JsonObject output);
    public void JsonDeserialize(JsonObject input);
}