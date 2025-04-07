struct GBufferOut {
    float4 COLOR_ROUGHNESS : SV_Target1;
    float4 LOCATION_METALLIC : SV_Target2;
    float4 NORMAL_SPECULAR : SV_Target3;

    [mutating]
    void setOutput(float3 color, float3 normal, float3 location, float roughness, float metallic, float specular) {
        this.COLOR_ROUGHNESS = float4(color, roughness);
        this.LOCATION_METALLIC = float4(location, metallic);
        this.NORMAL_SPECULAR = float4(normal, specular);
    }
};

