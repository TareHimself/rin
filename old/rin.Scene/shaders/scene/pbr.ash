#define RECIPROCAL_PI 0.3183098861837907
#define RECIPROCAL_2PI 0.15915494309189535

#include "scene.ash"
#include "normal.ash"


@Fragment {

    #include "rgb_lin.ash"

    float3 fresnelSchlick(float VdotH, float3 color){
        return color + (1.0 - color) * pow(1.0 - VdotH,5.0);
    }

    float D_GGX(float NdotH,float roughness){
        float r4 = roughness * roughness * roughness * roughness;
        float NdotH4 = NdotH * NdotH * NdotH * NdotH;
        float b = (NdotH4 * (r4 - 1.0) + 1.0);
        return r4 * RECIPROCAL_PI / (b * b);
    }
    
    float G1_GGX_Schlick(float NdotV, float roughness) {
        float k = (roughness * roughness) / 2.0;
        return max(NdotV, 0.001) / (NdotV * (1.0 - k) + k);
    }

    float G_Smith(float NdotV,float NdotL,float roughness){
        return G1_GGX_Schlick(NdotL,roughness) * G1_GGX_Schlick(NdotV,roughness);
    }

    float3 brdfMicrofacet(float3 L,float3 location,float3 color,float NdotV,float NdotL,float3 N,float3 V,float3 rms){
        float roughness = rms.r;
        float metallic = rms.g;
        float specular = rms.b;

        float3 H = normalize(V + L);

        float NdotH = clamp(dot(N,H),0.0,1.0);
        float VdotH = clamp(dot(V,H),0.0,1.0);
        
        float3 f0 = float3(0.16 * (specular * specular));
        
        f0 = mix(f0,color,metallic);

        float3 F = fresnelSchlick(VdotH,f0);
        float D = D_GGX(NdotH,roughness);
        float G = G_Smith(NdotV,NdotL,roughness);

        float3 spec = (F * D * G) / (4.0  * max(NdotV,0.001) * max(NdotL,0.001));

        float3 rhoD = color;

        rhoD *= (float3(1.0) - F)

        rhoD *= (1.0 - metallic);

        float3 diff = rhoD * RECIPROCAL_PI;

        return F;
    }

    float3 computeLightRadiance(float3 location,float3 color, Light light, float NdotV, float3 N, float3 V, float3 rms){
        float3 L = normalize(location - light.location.xyz);
        float irradiPerp = light.color.w;
        float NdotL = clamp(dot(N,L),0.0,1.0);
        float irradiance = NdotL * irradiPerp;
        if (irradiance > 0.0){

            float3 brdf = brdfMicrofacet(L,location,color,NdotV,NdotL,N,V,rms);

            return brdf * irradiance * light.color.xyz;
        }

        return float3(0.0);
    }

    float4 computePixelColor(float3 sceneLocation,float3 color,float3 normal,float3 rms,float3 emissive){
        float3 colorLin = rgb2lin(color);
        float3 V = normalize(scene.cameraLocation.xyz - sceneLocation);
        float NdotV = clamp(dot(normal, V), 0.0, 1.0);
        float3 radiance = rgb2lin(emissive);
        
        //return float4(normal,1.0);
        
        for (int i=0; i < scene.numLights; ++i)
        {
            radiance += computeLightRadiance(sceneLocation,colorLin, scene.lights[i], NdotV, normal, V,rms);
        }

        return float4(lin2rgb(radiance), 1.0);
    }
}
