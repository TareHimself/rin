#define RECIPROCAL_PI 0.3183098861837907
#define RECIPROCAL_2PI 0.15915494309189535

#include "scene.ash"
#include "normal.ash"


@Fragment {

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
    
    float3 disneyBrdf(float3 sceneLocation,float3 color,float3 lightDir, float NoV, float3 normal, float3 viewDir, float3 rms){
        float roughness = rms.x;
        float metallic = rms.y;
        float specular = rms.z;
        float3 H = normalize(viewDir + lightDir);
        float NoL = clamp(dot(normal, lightDir), 0.0, 1.0);
        float NoH = clamp(dot(normal, H), 0.0, 1.0);
        float VoH = clamp(dot(viewDir, H), 0.0, 1.0);

        float3 f0 = float3(0.16 * (specular * specular));

        f0 = mix(f0, color, metallic);

        float3 F = fresnelSchlick(VoH, f0);
        float D = D_GGX(NoH, roughness);
        float G = G_Smith(NoV, NoL, roughness);

        float3 spec = (F * D * G) / (4.0 * max(NoV, 0.001) * max(NoL, 0.001));

        float3 rhoD = color;

        rhoD *= float3(1.0) - F;

        rhoD *= disneyDiffuseFactor(NoV, NoL, VoH, roughness);

        rhoD *= (1.0 - metallic);

        float3 diff = rhoD * RECIPROCAL_PI;

        return (diff + spec);
    }

    float3 lambertDiff(float3 sceneLocation,float3 color,float3 lightDir, float NoV, float3 normal, float3 viewDir, float3 rms){
            return color;
    }

    float3 computeLightRadiance(float3 sceneLocation,float3 color, Light light, float NoV, float3 normal, float3 viewDir, float3 rms){
        //float3 lightDir = float3(0.0,0.0,-1.0);//normalize(sceneLocation - light.location.xyz);
        float3 lightDir = normalize(sceneLocation - light.location.xyz);
        float irradiPerp = light.color.w;
        float irradiAlpha = clamp(dot(normal,lightDir), 0.0, 1.0);
        float irradiance = irradiAlpha * irradiPerp;

        //return mix(float3(1.0,0.0,0.0),float3(0.0,1.0,0.0),irradiAlpha);
        if (irradiance > 0.0){

            float3 brdf = lambertDiff(sceneLocation,color, lightDir, NoV, normal, viewDir,rms);

            return brdf * irradiance * light.color.xyz;
        }

        return float3(0.0);
    }

    float4 computePixelColor(float3 sceneLocation,float3 color,float3 normal,float3 rms,float3 emissive){
        float3 viewDir = normalize(scene.cameraLocation.xyz - sceneLocation);
        float NoV = clamp(dot(normal, viewDir), 0.0, 1.0);
        float3 radiance = emissive;
        
        //return float4(normal,1.0);
        
        for (int i=0; i < scene.numLights; ++i)
        {
            radiance += computeLightRadiance(sceneLocation,color, scene.lights[i], NoV, normal, viewDir,rms);
        }

        return float4(radiance, 1.0);
    }
}
