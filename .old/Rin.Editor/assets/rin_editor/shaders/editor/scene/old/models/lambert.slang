import rin.scene.light;
#define PI 3.142
interface ILambertSurface : ILightSurface {
    float3 getColor();
    float3 getLocation();
    float3 getNormal();
};

struct LambertModel : ILightModel {

    typedef ILambertSurface Surface;

    float3 eval(Surface surface, float3 eye, ILight light) {
        var L = -getLightDirectionToLocation(light, surface.getLocation());

        var NoL = max(0.0f,dot(surface.getNormal(),L));
        
        return surface.getColor() / PI * light.getRadiance() * light.getColor() * NoL;
    }
}
