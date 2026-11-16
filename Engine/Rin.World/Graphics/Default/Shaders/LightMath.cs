using System.Numerics;
using Rin.Shade;
using Rin.World.Graphics.Surfaces;
using static Rin.Shade.Shader;

namespace Rin.World.Graphics.Default.Shaders;

[ShadeExport]
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

    /// <summary>
    ///     How much light the surface mirrors when viewed head on: a few percent for non-metals, the base colour for
    ///     metals.
    /// </summary>
    public static Vector3 BaseReflectivity(Surface surface)
    {
        var nonMetal = new Vector3(0.16f * (surface.Specular * surface.Specular));
        return Lerp(nonMetal, surface.BaseColor, surface.Metallic);
    }

    public static Vector3 Rgb2Lin(Vector3 rgb)
    {
        return Pow(rgb, new Vector3(2.2f));
    }

    public static Vector3 Lin2Rgb(Vector3 linear)
    {
        return Pow(linear, new Vector3(1f / 2.2f));
    }
}
