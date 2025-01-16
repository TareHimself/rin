import rin.scene.mesh.vertex;

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