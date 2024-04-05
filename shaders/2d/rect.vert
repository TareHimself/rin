#version 450
#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "ui.glsl"
#include "rect.glsl"



layout(location = 0) out vec2 oUV;



void main() 
{
    vec2 screenRes = ui.viewport.zw;
    vec4 extent = pRect.extent;
    vec2 normPt1 = normalizePoint(ui.viewport,extent.xy);
    vec2 normPt2 = normalizePoint(ui.viewport,extent.xy + extent.zw);
    vec2 size = normPt2 - normPt1;
    vec2 midpoint = normPt1 + (size / 2);

    vec2 vertex[] = { vec2(-0.5),vec2(0.5,-0.5),vec2(0.5),vec2(-0.5),vec2(0.5),vec2(-0.5,0.5) };

    vec4 vLoc = vec4(midpoint + (size * vertex[gl_VertexIndex]),0,0);
    
    gl_Position = vec4((pRect.transform * vLoc).xy,0,1);

    oUV = vertex[gl_VertexIndex] + 0.5;
}