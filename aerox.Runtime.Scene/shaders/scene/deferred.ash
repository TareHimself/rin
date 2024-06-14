#define RECIPROCAL_PI 0.3183098861837907
#define RECIPROCAL_2PI 0.15915494309189535
#define PI 3.14159265359
@Vertex {
    layout(location = 0) out float2 oUV;


    void main(){
        float2 normPt1 = float2(-1.0);
        float2 normPt2 = float2(1.0);
        float2 size = normPt2 - normPt1;
        float2 midpoint = normPt1 + (size / 2.0);

        // float2 vertex[] = { float2(-0.5),float2(0.5,-0.5),float2(0.5),float2(-0.5),float2(0.5),float2(-0.5,0.5) };
        float2 vertex[] = { float2(-0.5), float2(0.5, -0.5), float2(0.5), float2(-0.5), float2(0.5), float2(-0.5, 0.5) };

        gl_Position = float4(midpoint + (size * vertex[gl_VertexIndex]), 0, 1);

        // gl_Position = float4(vertex, 0.0, 1.0);

        oUV = vertex[gl_VertexIndex] + 0.5;
    }
}

@Fragment {
    #include "scene.ash"

    layout(set = 1, binding = 0) uniform sampler2D TColor;
    layout(set = 1, binding = 1) uniform sampler2D TLocation;
    layout(set = 1, binding = 2) uniform sampler2D TNormal;
    layout(set = 1, binding = 3) uniform sampler2D TRoughMetallicSpecular;
    layout(set = 1, binding = 5) uniform sampler2D TEmissive;

    layout(location = 0) in float2 iUV;
    layout(location = 0) out float4 oColor;

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

    float3 calcLightRadiance(float3 sceneLocation, float3 color, Light light, float NoV, float3 normal, float3 viewDir, float roughness, float metallic){
        float3 lightDirection = normalize(light.location.xyz - sceneLocation);//mix(iSceneLocation - light.location.xyz,light.direction.xyz,light.direction.w);
        float irradiPerp = light.color.w;
        float irradiance = max(dot(lightDirection, normal), 0.0) * irradiPerp;

        if (irradiance > 0.0){
            float3 H = normalize(viewDir + lightDirection);
            float NoL = clamp(dot(normal, lightDirection), 0.0001, 1.0);
            float NoH = clamp(dot(normal, H), 0.0001, 1.0);
            float VoH = clamp(dot(viewDir, H), 0.0001, 1.0);

            float3 f0 = float3(0.16 * (roughness * roughness));

            f0 = mix(f0, color, metallic);

            float3 F = fresnelSchlick(VoH, f0);
            float D = D_GGX(NoH, roughness);
            float G = G_Smith(NoV, NoL, roughness);

            float3 spec = (F * D * G) / (4.0 * max(NoV, 0.0001) * max(NoL, 0.0001));

            float3 rhoD = color;

            rhoD *= float3(1.0) - F;

            rhoD *= disneyDiffuseFactor(NoV, NoL, VoH, roughness);
            rhoD *= (1.0 - metallic);

            float3 diff = rhoD * RECIPROCAL_PI;

            return (diff + spec) * irradiance * light.color.xyz;
        }

        return float3(0.0);
    }
    
    /*float3 calcLambert(Light light,float3 color,float3 location,float3 normal, float3 cameraLocation,float3 roughMetalSpec){
        float3 lightPos = light.location.xyz;
        float3 L = normalize(lightPos - location);
        float3 lightDir = normalize(location - light.location.xyz);
        float3 result = float3(dot(normal,L)) * color * light.color.w;
        return result;
    }*/
    
    float3 calcLambert(Light light,float3 color,float3 location,float3 normal, float3 cameraLocation,float3 roughMetalSpec){
         float3 lightDirection = normalize(light.location.xyz - location);//mix(iSceneLocation - light.location.xyz,light.direction.xyz,light.direction.w);
         float irradiance = max(dot(normal,lightDirection), 0.0) * light.color.w;
         
         
         if (irradiance > 0.0){
         
            float3 cameraToLocation = normalize(scene.cameraLocation.xyz - location);
            float roughness = roughMetalSpec.r;
            float metallic = roughMetalSpec.g;
            
             float3 H = normalize(cameraToLocation + lightDirection);
             float NoL = clamp(dot(normal, lightDirection), 0.0001, 1.0);
             float NoH = clamp(dot(normal, H), 0.0001, 1.0);
             float VoH = clamp(dot(cameraToLocation, H), 0.0001, 1.0);
             float NoV = clamp(dot(normal, cameraToLocation), 0.0001, 1.0);
 
             float3 f0 = float3(0.16 * (roughness * roughness));
 
             f0 = mix(f0, color, metallic);
 
             float3 F = fresnelSchlick(VoH, f0);
             float D = D_GGX(NoH, roughness);
             float G = G_Smith(NoV, NoL, roughness);
 
             float3 spec = (F * D * G) / (4.0 * max(NoV, 0.0001) * max(NoL, 0.0001));
 
             float3 rhoD = color;
 
             rhoD *= float3(1.0) - F;
 
             rhoD *= disneyDiffuseFactor(NoV, NoL, VoH, roughness);
             rhoD *= (1.0 - metallic);
 
             float3 diff = rhoD * RECIPROCAL_PI;
 
             return (diff + spec) * irradiance * light.color.xyz;
         }
 
         return float3(0.0);
    }

    // Brffd Microfacet
    void main(){

        float3 sceneLocation = texture(TLocation, iUV).xyz;
        float3 color = texture(TColor, iUV).xyz;
        float3 normal = texture(TNormal, iUV).xyz;
        float3 roughMetallicSpecular = texture(TRoughMetallicSpecular, iUV).xyz;
        float3 emissive = texture(TEmissive, iUV).xyz;

        float3 viewDir = normalize(scene.cameraLocation.xyz - sceneLocation);
        float NoV = clamp(dot(normal, viewDir), 0.0001, 1.0);
        float3 radiance = emissive;

        for (int i=0; i < scene.numLights; ++i)
        {
            radiance += calcLambert(scene.lights[i],color,sceneLocation,normal,scene.cameraLocation.xyz,roughMetallicSpecular); //calcLightRadiance(sceneLocation, color, scene.lights[i], NoV, normal, viewDir, roughMetallicSpecular.r, roughMetallicSpecular.g);
        }

        oColor = float4(radiance, 1.0);
    }


}