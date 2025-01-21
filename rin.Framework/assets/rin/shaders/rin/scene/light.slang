enum LightType : int {
    Directional,
    Point,
};

struct Light {
    float3 location;
    float3 direction;
    float3 color;
    float radiance;
    LightType type;
};

interface ILightSurface {
}

interface ILightModel {
    associatedtype Surface : ILightSurface;
    float3 eval(Surface surface, float3 eye, Light light);
};