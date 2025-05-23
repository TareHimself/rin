#include "Engine/Shaders/utils.slang"
#include "Engine/Shaders/images.slang"
#include "Editor/Shaders/Mesh/vertex.slang"
#include "Editor/Shaders/scene_info.slang"
#include "Editor/Shaders/Lights/light.slang"
#include "Editor/Shaders/Lights/Models/pbr.slang"
// public Mat4 Transform = Mat4.Identity;
// public ulong VertexAddress = 0;
// private Vector4 _color_textureId = 0.0f;
// public int NormalTextureId = 0;
// private Vector4 _msre = 0.0f;
// private Vec4<int> _msreTextureId = 0;
struct PerMeshData {
    Vertex *vertices;
    float4x4 transform;
    float4 color_textureId;
    int normalTextureId;
    // Metallic Specular Roughness Emissive
    float4 MSRE;
    // Metallic Specular Roughness Emissive Texture Ids
    int4 msreTextureId;
    
    float3 getColor(float2 uv) {
        int id = (int)color_textureId.a;
        float3 value = color_textureId.rgb;

        if (id > 0) {
            value = sampleTexture(id, uv).rgb;
        }

        return value;
    }

    float getMetallic(float2 uv) {
        int id = msreTextureId.x;
        float value = MSRE.x;

        if (id > 0) {
            value = sampleTexture(id, uv).x;
        }

        return value;
    }

    float getSpecular(float2 uv) {
        int id = msreTextureId.y;
        float value = MSRE.y;

        if (id > 0) {
            value = sampleTexture(id, uv).y;
        }

        return value;
    }

    float getRoughness(float2 uv) {
        int id = msreTextureId.z;
        float value = MSRE.z;

        if (id > 0) {
            value = sampleTexture(id, uv).z;
        }

        return value;
    }

    float getEmissive(float2 uv) {
        int id = msreTextureId.w;
        float value = MSRE.w;

        if (id > 0) {
            value = sampleTexture(id, uv).w;
        }

        return value;
    }
};

struct PushConstants {
    SceneInfo *scene;
    PerMeshData *data;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

struct VertexIn {
    int vertexId : SV_VertexID;
    int instanceId : SV_InstanceID;
};

struct VertexOut {
    float2 uv : UV;
    float4 sceneLocation : SCENE_LOCATION;
    float4 sceneNormal : SCENE_NORMAL;
    float4 position : SV_Position;
    int instanceId : INSTANCE_ID;
};

struct FragmentIn {
    float2 uv : UV;
    float4 sceneLocation : SCENE_LOCATION;
    float4 sceneNormal : SCENE_NORMAL;
    float2 coordinate : SV_Position;
    int instanceId : INSTANCE_ID;
};

[shader("vertex")]
VertexOut vertex(VertexIn input) {
    var instanceData = push.data[input.instanceId];
    VertexOut output;
    var vertex = instanceData.vertices[input.vertexId];
    output.uv = vertex.getUV();
    output.sceneNormal = transpose(inverse(instanceData.transform)) * float4(vertex.getNormal(), 1.0);
    output.sceneLocation = instanceData.transform * float4(vertex.getLocation(), 1.0);
    output.position = push.scene->projection * push.scene->view * instanceData.transform * float4(vertex.getLocation(), 1.0);
    output.instanceId = input.instanceId;
    return output;
}

[shader("fragment")]
float4 fragment(FragmentIn input) : SV_Target
{

    var instanceData = push.data[input.instanceId];
    var eye = (push.scene->view * float4(float3(0.0f), 1.0f)).xyz;

    PBRModel model;

    PBRSurface surface;

    surface.location = input.sceneLocation.xyz;
    surface.normal = normalize(input.sceneNormal.xyz);
    surface.color = instanceData.getColor(input.uv);
    surface.roughness = instanceData.getRoughness(input.uv);
    surface.metallic = instanceData.getMetallic(input.uv);
    surface.specular = instanceData.getSpecular(input.uv);
    surface.emissive = instanceData.getEmissive(input.uv);

    float3 color = float3(surface.emissive);

    for (int i = 0; i < push.scene->numLights; i++) {
        color += model.eval(surface, eye, push.scene->lights[i]);
    }

    color /= push.scene->numLights;

    return float4(color, 1.0);
}