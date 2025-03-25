#ifndef RIN_LIGHT_TILE_MAX_BINS
#define RIN_LIGHT_TILE_MAX_BINS 1024
#endif
#ifndef RIN_LIGHT_TILE_SIZE
#define RIN_LIGHT_TILE_SIZE 16
#endif

struct LightTile {
    int count;
    int indices[RIN_LIGHT_TILE_MAX_BINS];
}

enum LightType : int {
    Directional,
    Point,
    Spot
};

interface ILight {
    float3 getLocation();
    float3 getDirection();
    float3 getColor();
    float getRadius();
    float getRadiance();
    LightType getType();
}

struct PackedLight : ILight {
    float4 locationRadius;
    float4 directionType;
    float4 colorRadiance;

    float3 getLocation() {
        return locationRadius.xyz;
    }
    float3 getDirection() {
        return directionType.xyz;
    }
    float3 getColor() {
        return colorRadiance.xyz;
    }
    float getRadius() {
        return locationRadius.w;
    }
    float getRadiance() {
        return colorRadiance.w;
    }
    LightType getType() {
        return (LightType)directionType.w;
    }
}

interface ILightSurface {

}

interface ILightModel {
    associatedtype Surface : ILightSurface;
    float3 eval(Surface surface, float3 eye, ILight light);
};

float3 getLightDirectionToLocation(ILight light, float3 location) {
    switch (light.getType()) {
    case LightType.Point:
        return normalize(location - light.getLocation());
    default:
        return light.getDirection();
    }
}

int computeLightTileIndex(float2 screenPosition) {

}