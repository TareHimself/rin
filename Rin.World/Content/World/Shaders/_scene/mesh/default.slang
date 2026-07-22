import rin.operators;
import rin.utils;
import rin.scene.mesh.vertex;
import rin.scene.mesh.gbuffer_out;
import rin.textures;

struct PerMeshData {
    float4x4 transform;
    Vertex *vertices;
    int baseColorTextureId;
    float4 baseColor;
    int normalTextureId;
    float metallic;
    float specular;
    float roughness;
    float emissive;
};

struct PushConstants {
    float4x4 view;
    float4x4 projection;
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
    output.uv = float2(vertex.location.w, vertex.normal.w);
    output.sceneNormal = transpose(inverse(instanceData.transform)) * float4(vertex.normal.xyz, 1.0);
    output.sceneLocation = instanceData.transform * float4(vertex.location.xyz, 1.0);
    output.position = push.projection * push.view * instanceData.transform * float4(vertex.location.xyz, 1.0);
    return output;
}

[shader("fragment")]
GBuffer fragment(FragmentIn input)
{
    GBuffer output;
    var instanceData = push.data[input.instanceId];
    var texColor = sampleTexture(0, input.uv);
    output.RGB_COLOR_A_ROUGHNESS = float4(texColor.xyz, instanceData.roughness);
    //output.RGB_COLOR_A_ROUGHNESS = float4(instanceData.baseColor.xyz, instanceData.roughness);
    output.RGB_LOCATION_A_METALLIC = float4(input.sceneLocation.xyz, instanceData.metallic);
    output.RGB_NORMAL_A_SPECULAR = float4(input.sceneNormal.xyz, instanceData.specular);
    return output;
}