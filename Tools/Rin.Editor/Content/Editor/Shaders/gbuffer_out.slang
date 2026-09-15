struct GBufferOut {
    float4 COLOR_ROUGHNESS : SV_Target1;
    float4 LOCATION_METALLIC : SV_Target2;
    float4 NORMAL_SPECULAR : SV_Target3;
    void setColor(float3 color) {

    }

    void setRoughness(float roughness) {

    }

    void setLocation(float3 location) {

    }

    void setMetallic(float metallic) {

    }

    void setSpecular(float specular) {
        NORMAL_SPECULAR[3] = specular;
    }
};

