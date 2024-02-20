#version 450

#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "ui.glsl"
#include "rect.glsl"

layout (location = 0) in vec2 iUV;
layout (location = 0) out vec4 oColor;


//https://iquilezles.org/articles/palettes/
vec3 palette( float t ) {
    vec3 a = vec3(0.5, 0.5, 0.5);
    vec3 b = vec3(0.5, 0.5, 0.5);
    vec3 c = vec3(1.0, 1.0, 1.0);
    vec3 d = vec3(0.263,0.416,0.557);
    // vec3 a = PushConstants.data1.rgb;
    // vec3 b = PushConstants.data2.rgb;
    // vec3 c = PushConstants.data3.rgb;
    // vec3 d = PushConstants.data3.rgb;

    return a + b*cos( 6.28318*(c*t+d) );
}

//https://www.shadertoy.com/view/mtyGWy
void main() {

    // if(shouldDiscard(ui.viewport,pRect.clip,gl_FragCoord.xy)){
    //     discard;
    // }

    ivec2 fragCoord = ivec2(gl_FragCoord.xy);
    ivec2 iResolution = ivec2(pRect.extent.zw);
    float iTime = ui.time.x;
	vec2 uv = (((fragCoord - pRect.extent.xy) * 2.0 - iResolution.xy) / iResolution.y);
    vec2 uv0 = uv;

	vec3 finalColor = vec3(0.0);
	
	for (float i = 0.0; i < 4.0; i++) {
		uv = fract(uv * 1.5) - 0.5;

		float d = length(uv) * exp(-length(uv0));

		vec3 col = palette(length(uv0) + i*.4 + iTime*.4);

		d = sin(d*8. + iTime)/8.;
		d = abs(d);

		d = pow(0.01 / d, 1.2);

		finalColor += col * d;
	}
	
	oColor = vec4(finalColor, 1.0);

    // if(fragCoord.x < iResolution.x && fragCoord.y < iResolution.y)
    // {
    //     vec2 uv = (fragCoord * 2.0 - iResolution.xy) / iResolution.y;
    //     vec2 uv0 = uv;
    //     vec3 finalColor = vec3(0.0);
        
    //     for (float i = 0.0; i < 4.0; i++) {
    //         uv = fract(uv * 1.5) - 0.5;

    //         float d = length(uv) * exp(-length(uv0));

    //         vec3 col = palette(length(uv0) + i*.4 + iTime*.4);

    //         d = sin(d*8. + iTime)/8.;
    //         d = abs(d);

    //         d = pow(0.01 / d, 1.2);

    //         finalColor += col * d;
    //     }
        
	// 	oColor = vec4(finalColor, 1.0);
    // }
    
}