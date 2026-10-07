using System.Numerics;
using ShadeMath = Rin.Shade.Shader.Math;

namespace Rin.World.Graphics.Default.Shaders;

// https://schuttejoe.github.io/post/disneybsdf/
public static class DisneyModel
{
    private const float ReciprocalPi = 0.3183098861837907f;

    private static Vector3 FresnelSchlick(float cosTheta, Vector3 f0)
    {
        return f0 + (new Vector3(1f) - f0) * ShadeMath.Pow(1f - cosTheta, 5f);
    }

    private static float GgxDistribution(float noH, float roughness)
    {
        var alpha = roughness * roughness;
        var alpha2 = alpha * alpha;
        var noH2 = noH * noH;
        var b = noH2 * (alpha2 - 1f) + 1f;
        return alpha2 * ReciprocalPi / (b * b);
    }

    private static float GeometrySchlick(float noV, float roughness)
    {
        var alpha = roughness * roughness;
        var k = alpha / 2f;
        return ShadeMath.Max(noV, 0.001f) / (noV * (1f - k) + k);
    }

    private static float SmithGeometry(float noV, float noL, float roughness)
    {
        return GeometrySchlick(noL, roughness) * GeometrySchlick(noV, roughness);
    }

    public static Vector3 Eval(GBufferSample surface, Vector3 eye, LightInfo light)
    {
        var location = surface.Location;
        var toSurface = LightMath.DirectionToLight(light, location);
        var normal = surface.Normal;

        var radiance = new Vector3(0f);
        var irradiance = light.Radiance *
                         LightMath.Attenuation(light, location);
        var noL = ShadeMath.Clamp(ShadeMath.Dot(normal, toSurface), 0f, 1f);
        
        if (ShadeMath.Min(noL,irradiance) > 0f)
        {
            var view = ShadeMath.Normalize(eye - location);
            var reflectance = surface.Specular;
            var roughness = surface.Roughness;
            var metallic = surface.Metallic;
            var baseColor = surface.Color;
            var halfway = ShadeMath.Normalize(view + toSurface);

            var noV = ShadeMath.Clamp(ShadeMath.Dot(normal, view), 0f, 1f);
            
            var noH = ShadeMath.Clamp(ShadeMath.Dot(normal, halfway), 0f, 1f);
            var voH = ShadeMath.Clamp(ShadeMath.Dot(view, halfway), 0f, 1f);

            var f0 = new Vector3(0.16f * (reflectance * reflectance));
            f0 = ShadeMath.Lerp(f0, baseColor, metallic);

            var fresnel = FresnelSchlick(voH, f0);
            var d = GgxDistribution(noH, roughness);
            var g = SmithGeometry(noV, noL, roughness);

            var specular = fresnel * d * g / (4f * ShadeMath.Max(noV, 0.001f) * ShadeMath.Max(noL, 0.001f));

            var rhoD = baseColor;
            rhoD *= new Vector3(1f) - fresnel;
            rhoD *= 1f - metallic;

            var diffuse = rhoD * ReciprocalPi;
            radiance += (diffuse + specular) * irradiance * light.Color * noL;
        }

        return radiance;
    }
}
