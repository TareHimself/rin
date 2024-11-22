// layout(location = 0) in vec3 iLocation;
// layout(location = 1) in vec3 iColor;
// layout(location = 2) in vec3 iNormal;
// layout(location = 3) in vec2 iUV;
#extension GL_EXT_scalar_block_layout : require
layout(set = 0, binding = 0, scalar) uniform  WidgetGlobalBuffer {
    float time;
    vec4 viewport;
    mat4 projection;
} ui;


