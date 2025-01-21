import rin.scene.light;
#define PI 3.142
struct LambertSurface : ILightSurface {
    float3 color;
    float3 location;
    float3 normal;
};

struct LambertModel : ILightModel {

    typedef LambertSurface Surface;

    float3 eval(Surface surface, float3 eye,Light light) {
        var L = light.direction;

        switch(light.type){
            case LightType.Point:
                L = normalize(surface.location - light.location);
                break;
            default:
            break;
        }

        L = -L;

        var NoL = max(0.0f,dot(surface.normal,L));
        
        return surface.color / PI * light.radiance * light.color * NoL;
    }
}