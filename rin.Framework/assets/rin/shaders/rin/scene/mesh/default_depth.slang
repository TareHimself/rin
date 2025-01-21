import rin.operators;
import rin.scene.mesh.vertex;
import rin.scene.mesh.mesh_depth;

struct PerMeshData {
    float4x4 transform;
    Vertex *vertices;
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
    float4 position : SV_Position;
};


[shader("vertex")]
VertexOut vertex(VertexIn input) {
    var instanceData = push.data[input.instanceId];
    VertexOut output;
    var vertex = instanceData.vertices[input.vertexId];
    output.position = push.projection * push.view * instanceData.transform * float4(vertex.location.xyz, 1.0);
    return output;
}
