using System.Text.Json.Nodes;

namespace Rin.Engine.Core.Extensions;

public static class JsonExtensions
{
    public static JsonObject ToJsonObject(this IJsonSerializable self)
    {
        var obj = new JsonObject();
        self.JsonSerialize(obj);
        return obj;
    }

    public static void Write(this JsonObject self, string key, IJsonSerializable src)
    {
        self[key] = src.ToJsonObject();
    }

    public static void Read(this JsonObject self, string key, IJsonSerializable dest)
    {
        if (self[key] is JsonObject obj) dest.JsonDeserialize(obj);
    }
}