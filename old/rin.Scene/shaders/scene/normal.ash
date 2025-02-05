mat3 cotangentFrame(in float3 N, in float3 p, in float2 uv)
{
    // get edge vectors of the pixel triangle
    float3 dp1 = dFdx(p);
    float3 dp2 = dFdy(p);
    float2 duv1 = dFdx(uv);
    float2 duv2 = dFdy(uv);

    // solve the linear system
    float3 dp2perp = cross(dp2, N);
    float3 dp1perp = cross(N, dp1);
    float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    float3 B = dp2perp * duv1.y + dp1perp * duv2.y;

    // construct a scale-invariant frame 
    float invmax = inversesqrt(max(dot(T, T), dot(B, B)));
    return mat3(T * invmax, B * invmax, N);
}

float3 applyNormalMap(sampler2D normalTexture, float3 normal, float3 viewVec, float2 texcoord)
{
    float3 highResNormal = texture(normalTexture, texcoord).xyz;
    highResNormal = normalize(highResNormal * 2.0 - 1.0);
    mat3 TBN = cotangentFrame(normal, -viewVec, texcoord);
    return normalize(TBN * highResNormal);
}