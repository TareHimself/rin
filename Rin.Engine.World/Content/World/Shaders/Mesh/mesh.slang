#include "Engine/Shaders/images.slang"
#include "Engine/Shaders/utils.slang"
#include "World/Shaders/Mesh/vertex.slang"
#include "World/Shaders/scene_info.slang"
#include "World/Shaders/gbuffer_out.slang"
#include "Engine/Shaders/Attributes/blending.slang"
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

        var handle = ImageHandle(id);
        if (handle.IsValid()) {
            value = sampleTexture(handle, uv).rgb;
        }

        return value;
    }

    float getMetallic(float2 uv) {
        int id = msreTextureId.x;
        float value = MSRE.x;

        var handle = ImageHandle(id);
        if (handle.IsValid()) {
            value = sampleTexture(handle, uv).r;
        }

        return value;
    }

    float getSpecular(float2 uv) {
        int id = msreTextureId.y;
        float value = MSRE.y;

        var handle = ImageHandle(id);
        if (handle.IsValid()) {
            value = sampleTexture(handle, uv).r;
        }

        return value;
    }

    float getRoughness(float2 uv) {
        int id = msreTextureId.z;
        float value = MSRE.z;

        var handle = ImageHandle(id);
        if (handle.IsValid()) {
            value = sampleTexture(handle, uv).r;
        }

        return value;
    }

    float getEmissive(float2 uv) {
        int id = msreTextureId.w;
        float value = MSRE.w;

        var handle = ImageHandle(id);
        if (handle.IsValid()) {
            value = sampleTexture(handle, uv).r;
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
    int vertexId : SV_VulkanVertexID;
    int instanceId : SV_VulkanInstanceID;
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
    var instanceId = input.instanceId;
    var instanceData = push.data[instanceId];
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
    output.instanceId = instanceId;
    return output;
}

[shader("fragment"),BlendOpaque,Depth]
GBuffer fragment(FragmentIn input)
{

    var instanceData = push.data[input.instanceId];
    var eye = push.scene->cameraPosition;

    var location = input.sceneLocation.xyz;
    var normal = normalize(input.sceneNormal.xyz);
    var color = ImageHandle(ImageType.Image, 0).SampleTexture(input.uv).rgb ;//instanceData.getColor(input.uv);
    var roughnes = instanceData.getRoughness(input.uv);
    var metallic = instanceData.getMetallic(input.uv);
    var specular = instanceData.getSpecular(input.uv);
    var emissive = instanceData.getEmissive(input.uv);

    return GBuffer(color,normal,location,roughnes,metallic,specular,emissive);
}