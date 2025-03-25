// Source https://youtu.be/gya7x9H3mV0?si=rHCphRwYF75nS4kt
#include "Engine/Shaders/utils.slang"
#include "Scene/Shaders/Lights/light.slang"


#define RECIPROCAL_PI 0.3183098861837907
#define RECIPROCAL_2PI 0.15915494309189535
#define PI 3.142
interface IPBRSurface : ILightSurface {
    float3 getColor();
    float3 getLocation();
    float3 getNormal();
    float getRoughness();
    float getMetallic();
    float getSpecular();
    float getEmissive();
};

struct PBRModel : ILightModel {

    typedef IPBRSurface Surface;

    float3 fresnelSchlick(float cosTheta, float3 F0) {
        return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
    }

    float D_GGX(float NoH, float roughness) {
        float alpha = roughness * roughness;
        float alpha2 = alpha * alpha;
        float NoH2 = NoH * NoH;
        float b = (NoH2 * (alpha2 - 1.0) + 1.0);
        return alpha2 * RECIPROCAL_PI / (b * b);
    }

    float G1_GGX_Schlick(float NoV, float roughness) {
        float alpha = roughness * roughness;
        float k = alpha / 2.0;
        return max(NoV, 0.001) / (NoV * (1.0 - k) + k);
    }

    float G_Smith(float NoV, float NoL, float roughness) {
        return G1_GGX_Schlick(NoL, roughness) * G1_GGX_Schlick(NoV, roughness);
    }

    float fresnelSchlick90(float cosTheta, float F0, float F90) {
        return F0 + (F90 - F0) * pow(1.0 - cosTheta, 5.0);
    }

    float disneyDiffuseFactor(float NoV, float NoL, float VoH, float roughness) {
        float alpha = roughness * roughness;
        float F90 = 0.5 + 2.0 * alpha * VoH * VoH;
        float F_in = fresnelSchlick90(NoL, 1.0, F90);
        float F_out = fresnelSchlick90(NoV, 1.0, F90);
        return F_in * F_out;
    }

    float3 eval(Surface surface, float3 eye, ILight light) {
        float3 L = -getLightDirectionToLocation(light, surface.getLocation());

        float3 N = surface.getNormal();

        float3 radiance = rgb2lin(float3(surface.getEmissive()));

        float irradiance = max(dot(L,N), 0.0) * light.getRadiance();

        if (irradiance > 0.0) { // if receives light
            float3 V = normalize(eye - surface.getLocation());
            
            float reflectance = surface.getSpecular();
            float roughness = surface.getRoughness();
            float metallic = surface.getMetallic();
            float3 baseColor = surface.getColor();
            float3 H = normalize(V + L);

            float NoV = clamp(dot(N, V), 0.0, 1.0);
            float NoL = clamp(dot(N, L), 0.0, 1.0);
            float NoH = clamp(dot(N, H), 0.0, 1.0);
            float VoH = clamp(dot(V, H), 0.0, 1.0);

            float3 f0 = float3(0.16 * (reflectance * reflectance));
            f0 = lerp(f0, baseColor, metallic);

            float3 F = fresnelSchlick(VoH, f0);
            float D = D_GGX(NoH, roughness);
            float G = G_Smith(NoV, NoL, roughness);

            float3 spec = (F * D * G) / (4.0 * max(NoV, 0.001) * max(NoL, 0.001));

            float3 rhoD = baseColor;

            // optionally
            rhoD *= float3(1.0) - F;
            // rhoD *= disneyDiffuseFactor(NoV, NoL, VoH, roughness);

            rhoD *= (1.0 - metallic);

            float3 diff = rhoD * RECIPROCAL_PI;

            float3 color = (diff + spec);

            // irradiance contribution from directional light
            radiance += color * irradiance * surface.getColor();
        }

        return lin2rgb(radiance);
    }
}