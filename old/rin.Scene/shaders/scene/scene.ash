struct Light {
    float4 location;
    float4 direction;
    float4 color;
};

layout(set = 0, binding = 0,scalar) uniform SceneGlobalBuffer {
    mat4 viewMatrix;
    mat4 projectionMatrix;
    float4 ambientColor;
    float4 lightDirection;
    float4 cameraLocation;
    int numLights;
    Light lights[1024];
} scene;
