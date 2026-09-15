
// #include "scene.ash"

// struct Vertex {
//     float4 location;
//     float4 normal;
// };

// layout(buffer_reference, std430) readonly buffer VertexBuffer {
//     Vertex vertices[];
// };

// push(scalar)
// {
//     mat4 transformMatrix;
//     VertexBuffer vertexBuffer;
// };

// @Vertex {

//     layout(location = 0) out float3 oSceneNormal;
//     layout(location = 1) out float2 oUV;
//     layout(location = 2) out float3 oSceneLocation;

//     void main()
//     {
//         Vertex v = push.vertexBuffer.vertices[gl_VertexIndex];
//         float4 location = float4(v.location.xyz, 1.0f);

//         mat4 viewProjection = scene.projectionMatrix * scene.viewMatrix;

//         float4 scenePosition = push.transformMatrix * location;

//         gl_Position = viewProjection * scenePosition;

//         // oSceneNormal = (push.transformMatrix * float4(v.normal.xyz, 1.f)).xyz;

//         oSceneNormal = -(transpose(inverse(mat3(push.transformMatrix))) * v.normal.xyz);

//         oUV = float2(v.location.w, v.normal.w);

//         oSceneLocation = scenePosition.xyz;
//     }
// }

interface IMeshVertexResult {

};

interface IMeshVertexProcessor<T : IMeshVertexResult> {
    IMeshVertexResult computeVertex();
};

interface IMeshShader {

}

import rin.attributes;

[shader("vertex")]
float4 vertex(int instanceId: SV_InstanceID, int vertexId: SV_VertexID, out float2 oUV: UV, out int oQuadIndex: QUAD_INDEX)
    : SV_Position
{
    QuadRenderInfo quad = push.quads[instanceId];
    float4 position;
    generateRectVertex(quad.size, push.projection, quad.transform, vertexId, position, oUV);
    oQuadIndex = instanceId;
    return position;
}