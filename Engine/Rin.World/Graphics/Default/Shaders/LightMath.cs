using System.Numerics;
using ShadeMath = Rin.Shade.Shader.Math;

namespace Rin.World.Graphics.Default.Shaders;

public static class LightMath
{
    public static Vector3 DirectionToLocation(LightInfo light, Vector3 location)
    {
        return light.LightType == LightType.Point
            ? ShadeMath.Normalize(location - light.Location)
            : ShadeMath.Normalize(light.Direction);
    }

    public static float Attenuation(LightInfo light, Vector3 location)
    {
        if (light.LightType == LightType.Directional) return 1f;

        var toLight = light.Location - location;
        var distanceSquared = ShadeMath.Max(ShadeMath.Dot(toLight, toLight), 0.0001f);
        var radius = ShadeMath.Max(light.Radius, 0.0001f);
        var window = ShadeMath.Saturate(1f - ShadeMath.Pow(distanceSquared / (radius * radius), 2f));
        return window * window / distanceSquared;
    }

    public static Vector3 Rgb2Lin(Vector3 rgb)
    {
        return ShadeMath.Pow(rgb, new Vector3(2.2f));
    }

    public static Vector3 Lin2Rgb(Vector3 linear)
    {
        return ShadeMath.Pow(linear, new Vector3(1f / 2.2f));
    }
}
