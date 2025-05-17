#include "Engine/Shaders/images.slang"
#include "Engine/Shaders/utils.slang"
#include "World/Shaders/Lights/Models/lambert.slang"
#include "World/Shaders/Lights/Models/pbr_new.slang"
#include "World/Shaders/Lights/light.slang"
#include "World/Shaders/Mesh/vertex.slang"
#include "World/Shaders/gbuffer_out.slang"
#include "World/Shaders/scene_info.slang"


// public Mat4 Transform = Mat4.Identity;
// public ulong VertexAddress = 0;
// private Vector4 _color_textureId = 0.0f;
// public int NormalTextureId = 0;
// private Vector4 _msre = 0.0f;
// private Vec4<int> _msreTextureId = 0;
struct PerMeshData {
    Vertex *vertices;
    float4x4 transform;
    float4 color;
    ImageHandle colorHandle;
    ImageHandle normalHandle;
    ImageHandle metallicRoughnessHandle;
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
    float3 sceneLocation : SCENE_LOCATION;
    float3 sceneNormal : SCENE_NORMAL;
    float4 position : SV_Position;
    int instanceId : INSTANCE_ID;
};

struct FragmentIn {
    float2 uv : UV;
    float3 sceneLocation : SCENE_LOCATION;
    float3 sceneNormal : SCENE_NORMAL;
    float2 coordinate : SV_Position;
    int instanceId : INSTANCE_ID;
};

[shader("vertex")]
VertexOut vertex(VertexIn input) {
    var instanceData = push.data[input.instanceId];
    VertexOut output;
    var vertex = instanceData.vertices[input.vertexId];
    var location4 = float4(vertex.getLocation(), 1.0);
    var sceneLocation = instanceData.transform * location4;
    output.uv = vertex.getUV();
    output.sceneNormal = transformNormal(vertex.getNormal(), instanceData.transform); // transpose(inverse(instanceData.transform)) * float4(vertex.getNormal(), 1.0);
    output.sceneLocation = sceneLocation.xyz;   
    // We do this separately because of depth precision https://developer.nvidia.com/blog/visualizing-depth-precision/
    var viewLocation = push.scene->view * float4(sceneLocation.xyz, 1.0);
    output.position = push.scene->projection * viewLocation;
    output.instanceId = input.instanceId;
    return output;
}

struct MeshSurface : ILambertSurface, IPBRSurface {
    float3 color;
    float3 location;
    float3 normal;
    float roughness;
    float metallic;
    float specular;
    float emissive;

    float3 getColor() {
        return color;
    }
    float3 getLocation() {
        return location;
    }
    float3 getNormal() {
        return normal;
    }

    float getRoughness() {
        return roughness;
    }
    float getMetallic() {
        return metallic;
    }
    float getSpecular() {
        return specular;
    }
    float getEmissive() {
        return emissive;
    }
}
[shader("fragment")]
GBuffer fragment(FragmentIn input) : SV_Target
{

    var instanceData = push.data[input.instanceId];
    var eye = push.scene->cameraPosition;

    //return float4(input.uv, float2(1.0, 1.0));
    // return float4(instanceData.getColor(input.uv),1.0);
    PBRModel model;
    MeshSurface surface;
    var color = instanceData.colorHandle.SampleTexture(input.uv).rgb;
    var metallicRoughness = instanceData.metallicRoughnessHandle.SampleTexture(input.uv).rg;
    var location = input.sceneLocation.xyz;
    var normal = normalize(input.sceneNormal.xyz);

    return GBuffer(color,normal,location,metallicRoughness.g,metallicRoughness.r,0.0,0.0);
}