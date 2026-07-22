struct Bounds1D {
    float Min;
    float Max;
};
struct Bounds3D {
    float3 Min;
    float3 Max;

    __init(float3 location) {
        Min = location;
        Max = location;
    }
    [mutating]
    void Update(float3 location) {
        Min = min(Min, location);
        Max = max(Max, location); 
    }
}