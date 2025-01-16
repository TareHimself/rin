import rin.operators;
import rin.scene.mesh.mesh_depth;
import rin.scene.mesh.default_data;

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
    output.sceneNormal = instanceData.transform * float4(vertex.normal.xyz, 1.0);
    output.sceneLocation = instanceData.transform * float4(vertex.location.xyz, 1.0);
    output.position = push.projection * push.view * instanceData.transform * float4(vertex.location.xyz, 1.0);
    return output;
}

[shader("fragment")]
FragmentOut fragment(FragmentIn input)
{
    FragmentOut output;
    output.OUTPUT = float4(1.0);
    return output;
}