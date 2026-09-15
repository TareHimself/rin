using System.Numerics;
using System.Text.Json.Nodes;
using Rin.Core.Graphics;

namespace Rin.Core.Extensions;

public static class JsonExtensions
{
    public static JsonObject ToJsonObject(this IJsonSerializable self)
    {
        var obj = new JsonObject();
        self.JsonSerialize(obj);
        return obj;
    }

    extension(JsonObject self)
    {
        public void Write(string key, IJsonSerializable src)
        {
            self[key] = src.ToJsonObject();
        }

        public void Read(string key, IJsonSerializable dest)
        {
            if (self[key] is JsonObject obj) dest.JsonDeserialize(obj);
        }

        public Vector3 ToVector3()
        {
            return new Vector3
            {
                X = self["X"]?.GetValue<float>() ?? 0,
                Y = self["Y"]?.GetValue<float>() ?? 0,
                Z = self["Z"]?.GetValue<float>() ?? 0
            };
        }

        public Extent2D ToExtent2D()
        {
            return new Extent2D
            {
                Width = self["Width"]?.GetValue<uint>() ?? 0,
                Height = self["Height"]?.GetValue<uint>() ?? 0
            };
        }

        public Vector4 ToVector4()
        {
            return new Vector4
            {
                X = self["X"]?.GetValue<float>() ?? 0,
                Y = self["Y"]?.GetValue<float>() ?? 0,
                Z = self["Z"]?.GetValue<float>() ?? 0,
                W = self["W"]?.GetValue<float>() ?? 0
            };
        }

        public Vector2 ToVector2()
        {
            return new Vector2
            {
                X = self["X"]?.GetValue<float>() ?? 0,
                Y = self["Y"]?.GetValue<float>() ?? 0
            };
        }
    }

    public static JsonObject ToJson(this in Vector2 self)
    {
        return new JsonObject
        {
            ["X"] = self.X,
            ["Y"] = self.Y
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

    public static JsonObject ToJson(this in Extent2D self)
    {
        return new JsonObject
        {
            ["Width"] = self.Width,
            ["Height"] = self.Height
        };
    }
}