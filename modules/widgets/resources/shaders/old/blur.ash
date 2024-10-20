#include "widget.rsl"
#include "utils.rsl"

push(scalar){
    mat3 transform;
    float2 size;
    float blurRadius;
    float4 tint;
};

@Vertex {
    layout(location = 0) out float2 oUV;


    void main()
    {
        generateRectVertex(push.size, ui.projection, push.transform, gl_VertexIndex, gl_Position, oUV);
    }
}

@Fragment {

    layout(set = 1, binding = 0) uniform sampler2D SourceT;
    layout(location = 0) in float2 iUV;
    layout(location = 0) out float4 oColor;

    float normpdf(in float x, in float sigma)
    {
        return 0.39894*exp(-0.5*x*x/(sigma*sigma))/sigma;
    }

    // https://www.shadertoy.com/view/XdfGDH
    void main() {
        //    if (shouldDiscard(ui.viewport, push.clip, gl_FragCoord.xy)){
        //        discard;
        //    }
        //float2 uv = float2(mapRangeUnClamped(iUV.x,0.0,1.0,0.5,1.0),mapRangeUnClamped(iUV.y,0.0,1.0,0.5,1.0));
        float2 imSize = float2(textureSize(SourceT, 0));
        float2 actualUv = gl_FragCoord.xy / imSize;
        float3 c = texture(SourceT, actualUv).rgb;
        //oColor = float4(1.0,0.0,0.0, 1.0);

        //declare stuff
        const int mSize = 11;
        const int kSize = (mSize-1)/2;
        float kernel[11];
        float3 finalColor = float3(0.0);

        //create the 1-D kernel
        float sigma = 7.0;
        float Z = 0.0;
        for (int j = 0; j <= kSize; ++j)
        {
            kernel[kSize+j] = kernel[kSize-j] = normpdf(float(j), sigma);
        }

        //get the normalization factor (as the gaussian has been clamped)
        for (int j = 0; j < mSize; ++j)
        {
            Z += kernel[j];
        }

        //read out the texels
        for (int i=-kSize; i <= kSize; ++i)
        {
            for (int j=-kSize; j <= kSize; ++j)
            {
                finalColor += kernel[kSize+j]*kernel[kSize+i]*texture(SourceT, (gl_FragCoord.xy+float2(float(i), float(j))) / imSize).rgb;
            }
        }

        finalColor /= (Z*Z);

        oColor = float4(finalColor, 1.0) * push.tint;
    }
}