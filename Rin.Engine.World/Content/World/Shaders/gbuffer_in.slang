[[vk::binding(0, 0)]]
uniform Sampler2D COLOR_ROUGHNESS;

[[vk::binding(1, 0)]]
uniform Sampler2D LOCATION_METALLIC;

[[vk::binding(2, 0)]]
uniform Sampler2D NORMAL_SPECULAR;


struct GBufferIn {
    float getColor() {
        
    }
};

