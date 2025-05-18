#include "Engine/Shaders/operators.slang"
#include "World/Shaders/Mesh/vertex.slang"
#include "World/Shaders/depth_scene_info.slang"
#include "Engine/Shaders/Attributes/blending.slang"

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


[shader("vertex"),Depth]
VertexOut vertex(VertexIn input) {
    var instanceData = push.data[input.instanceId];
    VertexOut output;
    var vertex = instanceData.vertices[input.vertexId];
    // We do this separately because of depth precision https://developer.nvidia.com/blog/visualizing-depth-precision/
    var sceneLocation = instanceData.transform * float4(vertex.getLocation(), 1.0);
    var viewLocation = push.scene->view * sceneLocation;
    output.position = push.scene->projection * viewLocation;
    return output;
}
