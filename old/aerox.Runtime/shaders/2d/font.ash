#include "widget.ash"
#include "utils.ash"

push(scalar){
    mat3 transform;
    float2 size;
    int atlasIdx;
    int4 rect;
};

@Vertex {
    layout(location = 0) out float2 oUV;


    void main()
    {
        generateRectVertex(push.size, ui.projection, push.transform, gl_VertexIndex, gl_Position, oUV);
    }
}

@Fragment {

    
    layout (location = 0) in float2 iUV;
    layout (location = 0) out float4 oColor;

    layout(set = 1, binding = 0) uniform sampler2D TAtlas[12];

    float median(float r, float g, float b) {
        return max(min(r, g), min(max(r, g), b));
    }

    layout(set = 1, binding = 1, scalar) uniform OptionsUniform{
        float4 bg;
        float4 fg;
    } options;

    float screenPxRange(float2 uv) {
        float2 unitRange = float2(30.0)/float2(push.rect.zw);
        float2 screenTexSize = float2(1.0)/fwidth(uv);
        return max(0.5*dot(unitRange, screenTexSize), 1.0);
    }

    void main()
    {
        
        float4 bgColor = options.bg;

        float4 fgColor = options.fg;
        
        float2 uv = iUV;

        float2 atlasSize = float2(textureSize(TAtlas[push.atlasIdx], 0));
        
        float4 rectInAtlas = float4(float2(push.rect.xy) / atlasSize,float2(push.rect.xy + push.rect.zw) / atlasSize);
        
        float2 actualUv = float2(mapRangeUnClamped(uv.x,0.0,1.0,rectInAtlas.x,rectInAtlas.z),mapRangeUnClamped(uv.y,1.0,0.0,rectInAtlas.y,rectInAtlas.w));
        
        float3 msd = texture(TAtlas[push.atlasIdx], actualUv).rgb;
        
        float sd = median(msd.r, msd.g, msd.b);
        
        float screenPxDistance = screenPxRange(uv)*(sd - 0.5);
        
        float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);

        oColor = mix(fgColor, bgColor, opacity);
    }
}