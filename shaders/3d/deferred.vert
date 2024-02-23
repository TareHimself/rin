#version 450

layout(location = 0) out vec2 oUV;


void main(){
    vec2 normPt1 = vec2(-1.0);
    vec2 normPt2 = vec2(1.0);
    vec2 size = normPt2 - normPt1;
    vec2 midpoint = normPt1 + (size / 2.0);

    // vec2 vertex[] = { vec2(-0.5),vec2(0.5,-0.5),vec2(0.5),vec2(-0.5),vec2(0.5),vec2(-0.5,0.5) };
    vec2 vertex[] = { vec2(-0.5),vec2(0.5,-0.5),vec2(0.5),vec2(-0.5),vec2(0.5),vec2(-0.5,0.5) };

    gl_Position = vec4(midpoint + (size * vertex[gl_VertexIndex]),0,1);

    // gl_Position = vec4(vertex, 0.0, 1.0);
    
    oUV = vertex[gl_VertexIndex] + 0.5;
}