using System.Numerics;
using System.Text.Json.Nodes;
using Rin.Framework.Graphics;

namespace Rin.Framework.Extensions;

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

    public static JsonObject ToJson(this in Vector2 self)
    {
        return new JsonObject
        {
            ["X"] = self.X,
            ["Y"] = self.Y
        };
    }

    public static Vector2 ToVector2(this JsonObject self)
    {
        return new Vector2
        {
            X = self["X"]?.GetValue<float>() ?? 0,
            Y = self["Y"]?.GetValue<float>() ?? 0
        };
    }

    public static JsonObject ToJson(this in Vector3 self)
    {
        return new JsonObject
        {
            ["X"] = self.X,
            ["Y"] = self.Y,
            ["Z"] = self.Z
        };
    }

    public static Vector3 ToVector3(this JsonObject self)
    {
        return new Vector3
        {
            X = self["X"]?.GetValue<float>() ?? 0,
            Y = self["Y"]?.GetValue<float>() ?? 0,
            Z = self["Z"]?.GetValue<float>() ?? 0
        };
    }

    public static JsonObject ToJson(this in Vector4 self)
    {
        return new JsonObject
        {
            ["X"] = self.X,
            ["Y"] = self.Y,
            ["Z"] = self.X,
            ["W"] = self.Y
        };
    }

    public static Vector4 ToVector4(this JsonObject self)
    {
        return new Vector4
        {
            X = self["X"]?.GetValue<float>() ?? 0,
            Y = self["Y"]?.GetValue<float>() ?? 0,
            Z = self["Z"]?.GetValue<float>() ?? 0,
            W = self["W"]?.GetValue<float>() ?? 0
        };
    }

    public static JsonObject ToJson(this in Extent2D self)
    {
        return new JsonObject
        {
            ["Width"] = self.Width,
            ["Height"] = self.Height
        };
    }

    public static Extent2D ToExtent2D(this JsonObject self)
    {
        return new Extent2D
        {
            Width = self["Width"]?.GetValue<uint>() ?? 0,
            Height = self["Height"]?.GetValue<uint>() ?? 0
        };
    }
}