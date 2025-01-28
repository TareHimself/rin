import rin.operators;
import rin.utils;
import rin.scene.scene_info;
import rin.scene.mesh.vertex;
import rin.scene.mesh.gbuffer_out;
import rin.textures;
import rin.scene.light;
import rin.scene.models.pbr;

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
    output.uv = float2(vertex.location.w, vertex.normal.w);
    output.sceneNormal = transpose(inverse(instanceData.transform)) * float4(vertex.normal.xyz, 1.0);
    output.sceneLocation = instanceData.transform * float4(vertex.location.xyz, 1.0);
    output.position = push.scene->projection * push.scene->view * instanceData.transform * float4(vertex.location.xyz, 1.0);
    return output;
}

[shader("fragment")]
float4 fragment(FragmentIn input) : SV_Target
{
    
    var instanceData = push.data[input.instanceId];
    var texColor = sampleTexture(0, input.uv);

    var eye = (push.scene->view * float4(float3(0.0f), 1.0f)).xyz;

    PBRModel model;

    PBRSurface surface;

    surface.location = input.sceneLocation.xyz;
    surface.normal = normalize(input.sceneNormal.xyz);
    surface.color = texColor.xyz;
    surface.roughness = instanceData.roughness;
    surface.metallic = instanceData.metallic;
    surface.specular = instanceData.specular;
    surface.emissive = instanceData.emissive;

    float3 color = float3(surface.emissive);

    for (int i = 0; i < push.scene->numLights; i++) {
        color += model.eval(surface, eye, push.scene->lights[i]);
    }

    color /= push.scene->numLights;

    return float4(color, 1.0);
}