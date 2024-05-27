#version 450

#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "ui.glsl"
#include "blur.glsl"

layout(set = 1, binding = 0) uniform sampler2D SourceT;
layout (location = 0) in vec2 iUV;
layout (location = 0) out vec4 oColor;

float normpdf(in float x, in float sigma)
{
    return 0.39894*exp(-0.5*x*x/(sigma*sigma))/sigma;
}

// https://www.shadertoy.com/view/XdfGDH
void main() {
    //    if (shouldDiscard(ui.viewport, push.clip, gl_FragCoord.xy)){
    //        discard;
    //    }
    //vec2 uv = vec2(mapRangeUnClamped(iUV.x,0.0,1.0,0.5,1.0),mapRangeUnClamped(iUV.y,0.0,1.0,0.5,1.0));
    vec2 imSize = vec2(textureSize(SourceT, 0));
    vec2 actualUv = gl_FragCoord.xy / imSize;
    vec3 c = texture(SourceT, actualUv).rgb;
    //oColor = vec4(1.0,0.0,0.0, 1.0);

    //declare stuff
    const int mSize = 11;
    const int kSize = (mSize-1)/2;
    float kernel[mSize];
    vec3 finalColor = vec3(0.0);

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
            finalColor += kernel[kSize+j]*kernel[kSize+i]*texture(SourceT, (gl_FragCoord.xy+vec2(float(i), float(j))) / imSize).rgb;
        }
    }

    finalColor /= (Z*Z);

    oColor = vec4(finalColor, 1.0) * push.tint;
}