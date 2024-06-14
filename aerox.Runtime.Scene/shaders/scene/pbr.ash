#define RECIPROCAL_PI 0.3183098861837907
#define RECIPROCAL_2PI 0.15915494309189535

layout (location = 0) in float3 iSceneNormal;
layout (location = 1) in float2 iUV;
layout (location = 2) in float3 iSceneLocation;

struct Light {
    float4 location;
    float4 direction;
    
    float4 color; //[r,g,b,intensity]
};

layout(set = 0, binding = 0) uniform  SceneGlobalBuffer {
    mat4 viewMatrix;
    mat4 projectionMatrix;
    float4 ambientColor;
    float4 lightDirection;
    float4 cameraLocation;
    float4 numLights;
    Light lights[1024];
} scene;

// from http://www.thetenthplanet.de/archives/1180
mat3 cotangentFrame(in float3 N, in float3 p, in float2 uv)
{
    // get edge vectors of the pixel triangle
    float3 dp1 = dFdx(p);
    float3 dp2 = dFdy(p);
    float2 duv1 = dFdx(uv);
    float2 duv2 = dFdy(uv);

    // solve the linear system
    float3 dp2perp = cross(dp2, N);
    float3 dp1perp = cross(N, dp1);
    float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    float3 B = dp2perp * duv1.y + dp1perp * duv2.y;

    // construct a scale-invariant frame 
    float invmax = inversesqrt(max(dot(T, T), dot(B, B)));
    return mat3(T * invmax, B * invmax, N);
}

float3 applyNormalMap(sampler2D normalTexture, float3 normal, float3 viewVec, float2 texcoord)
{
    float3 highResNormal = texture(normalTexture, texcoord).xyz;
    highResNormal = normalize(highResNormal * 2.0 - 1.0);
    mat3 TBN = cotangentFrame(normal, -viewVec, texcoord);
    return normalize(TBN * highResNormal);
}


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

float3 calcLightRadiance(float3 color, Light light, float NoV, float3 normal, float3 viewDir, float roughness, float metallic){
    float3 lightDirection = iSceneLocation - light.location.xyz;
    float3 lightDir = normalize(-lightDirection);
    float irradiPerp = light.color.w;
    float irradiance = max(dot(lightDir, normal), 0.0) * irradiPerp;

    if (irradiance > 0.0){
        float3 H = normalize(viewDir + lightDir);
        float NoL = clamp(dot(normal, lightDir), 0.0, 1.0);
        float NoH = clamp(dot(normal, H), 0.0, 1.0);
        float VoH = clamp(dot(viewDir, H), 0.0, 1.0);

        float3 f0 = float3(0.16 * (roughness * roughness));

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

        return (diff + spec) * irradiance * light.color.xyz;
    }

    return float3(0.0);
}

// Brffd Microfacet
float3 computeColor(float3 color, float3 normal, float roughness, float metallic, float3 specular, float3 emissive){
    float3 viewDir = normalize(scene.cameraLocation.xyz - iSceneLocation);
    float NoV = clamp(dot(normal, viewDir), 0.0, 1.0);
    float3 radiance = emissive;
    int numLights = int(scene.numLights.x);

    for (int i=0; i < numLights; ++i)
    {
        radiance += calcLightRadiance(color, scene.lights[i], NoV, normal, viewDir, roughness, metallic);
    }

    return radiance;
}