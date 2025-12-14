#include "Framework/Shaders/Attributes/blending.slang"
struct GBuffer {
    [Attachment(AttachmentFormat.RGBA32)]
    float4 G_BUFFER_0 : SV_Target1;
    [Attachment(AttachmentFormat.RGBA32)]
    float4 G_BUFFER_1 : SV_Target2;
    [Attachment(AttachmentFormat.RGBA32)]
    float4 G_BUFFER_2 : SV_Target3;

    [mutating]
    __init(float3 color, float3 normal, float3 location, float roughness, float metallic, float specular, float emissive) {
        this.G_BUFFER_0 = float4(color, roughness);
        this.G_BUFFER_1 = float4(location, metallic);
        this.G_BUFFER_2 = float4(normal, specular);
    }
};

