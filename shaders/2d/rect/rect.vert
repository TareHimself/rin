#version 450
#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "../ui.glsl"

layout(location = 0) out vec4 oColor;

// Constant rectangle vertices in normalized device coordinates (NDC)
const vec2 vertices[6] = vec2[](
    vec2(-0.5, -0.5),   // Vertex 1
    vec2( 0.5, -0.5),   // Vertex 2
    vec2( 0.5,  0.5),   // Vertex 3
    vec2(-0.5, -0.5),   // Vertex 1 (repeated for triangle fan)
    vec2( 0.5,  0.5),   // Vertex 3 (repeated for triangle fan)
    vec2(-0.5,  0.5)    // Vertex 4
);

//push constants block
layout( push_constant ) uniform constants
{
	vec4 extent;
} pRect;

void main() 
{
    vec2 screenRes = ui.viewport.zw;
	// Transform vertices based on input extent
    vec4 extent = pRect.extent;
    // vec2 scaledVertices[6];
    // for (int i = 0; i < 6; ++i) {
    //     scaledVertices[i] = vertices[i];// * vec2(extent.z, extent.w) + vec2(extent.x, extent.y);
    // }

    // Output vertex positions
    gl_Position = vec4(vertices[gl_VertexIndex], 0.0, 1.0);
    
    // Assign a color for visualization
    oColor = vec4(1.0, 0.0, 0.0, 1.0); // Red color
}