#include "Engine/Shaders/operators.slang"
#include "Scene/Shaders/Mesh/vertex.slang"
#include "Scene/Shaders/depth_scene_info.slang"


struct PerMeshData {
    float4x4 transform;
    Vertex *vertices;
};

struct PushConstants {
    DepthSceneInfo *scene;
    PerMeshData *data;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

struct VertexIn {
    int vertexId : SV_VertexID;
    int instanceId : SV_InstanceID;
};

struct VertexOut {
    float4 position : SV_Position;
};


[shader("vertex")]
VertexOut vertex(VertexIn input) {
    var instanceData = push.data[input.instanceId];
    VertexOut output;
    var vertex = instanceData.vertices[input.vertexId];
    output.position = push.scene->projection * push.scene->view * instanceData.transform * float4(vertex.getLocation(), 1.0);
    return output;
}
