#version 450
#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "ui.glsl"
#include "rect.glsl"



layout(location = 0) out vec2 oUV;



void main() 
{
    vec2 screenRes = ui.viewport.zw;
    
	// Transform vertices based on input extent
    vec4 extent = pRect.extent;
    // float xLeft = mapRangeUnClamped(extent.x,0.0,screenRes.x,-1.0,1.0);
    // float xRight = mapRangeUnClamped(extent.x + extent.z,0.0,screenRes.x,-1.0,1.0);
    // float yBottom = mapRangeUnClamped(extent.y + extent.w,0.0,screenRes.y,-1.0,1.0);
    float yTop = mapRangeUnClamped(extent.y,0.0,screenRes.y,-1.0,1.0);
    float isLeft = float (gl_VertexIndex == 0 || gl_VertexIndex == 3 || gl_VertexIndex == 5);
    float isTop = float (gl_VertexIndex == 0 || gl_VertexIndex == 1 || gl_VertexIndex == 3);
    //vec2 vertex = vec2(mix(xLeft,xRight,isLeft),mix(yTop,yBottom,isTop));//vertices[gl_VertexIndex];

    vec2 normPt1 = normalizePoint(ui.viewport,extent.xy);
    vec2 normPt2 = normalizePoint(ui.viewport,extent.xy + extent.zw);
    vec2 size = normPt2 - normPt1;
    vec2 midpoint = normPt1 + (size / 2);

    // vec2 vertex[] = { vec2(-0.5),vec2(0.5,-0.5),vec2(0.5),vec2(-0.5),vec2(0.5),vec2(-0.5,0.5) };
    vec2 vertex[] = { vec2(-0.5),vec2(0.5,-0.5),vec2(0.5),vec2(-0.5),vec2(0.5),vec2(-0.5,0.5) };

    vec4 vLoc = vec4(midpoint + (size * vertex[gl_VertexIndex]),0,0);
    
    gl_Position = vec4((pRect.transform * vLoc).xy,0,1);

    // gl_Position = vec4(vertex, 0.0, 1.0);
    
    oUV = vertex[gl_VertexIndex] + 0.5;
}