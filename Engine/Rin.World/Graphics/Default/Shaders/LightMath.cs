using System.Numerics;
using static Rin.Shade.Shader;

namespace Rin.World.Graphics.Default.Shaders;

public static class LightMath
{
    public static Vector3 DirectionToLight(LightInfo light, Vector3 location)
    {
        return light.LightType == LightType.Point
            ? Normalize(light.Location - location)
            : Normalize(-light.Direction);
    }

    public static float Attenuation(LightInfo light, Vector3 location)
    {
        if (light.LightType == LightType.Directional) return 1f;

        var toLight = light.Location - location;
        var distanceSquared = Max(Dot(toLight, toLight), 0.0001f);
        var radius = Max(light.Radius, 0.0001f);
        var window = Saturate(1f - Pow(distanceSquared / (radius * radius), 2f));
        return window * window / distanceSquared;
    }

    public static Vector3 Rgb2Lin(Vector3 rgb)
    {
        return Pow(rgb, new Vector3(2.2f));
    }

    public static Vector3 Lin2Rgb(Vector3 linear)
    {
        return Pow(linear, new Vector3(1f / 2.2f));
    }
    
    public static Vector3 AcesFilm(Vector3 x)
    {
        const float a = 2.51f;
        const float b = 0.03f;
        const float c = 2.43f;
        const float d = 0.59f;
        const float e = 0.14f;
    
        // Formula: (x * (a * x + b)) / (x * (c * x + d) + e)
        var numerator = x * (new Vector3(a) * x + new Vector3(b));
        var denominator = x * (new Vector3(c) * x + new Vector3(d)) + new Vector3(e);
    
        return Saturate(numerator / denominator);
    }
}
