enum LightType : int {
    Directional,
    Point,
}

struct Light {
    float3 location;
    float3 direction;
    float4 color;
    LightType type;
}

struct SceneBuffer {
    float4x4 viewMatrix;
    float4x4 projectionMatrix;
    float4 viewPoint;
    int numLights;
    Light lights[];
}

interface IBrdf {
    float4 apply(float4 location);
}