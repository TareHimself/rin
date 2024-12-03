#include "widget.rsl"
#include "utils.rsl"

push(scalar)
{
    mat3 transform;
    float2 size;
    float4 borderRadius;
};

@Vertex {
    layout(location = 0) out float2 oUV;

    void main()
    {
        generateRectVertex(push.size, ui.projection, push.transform, gl_VertexIndex, gl_Position, oUV);
    }
}

@Fragment {
    layout(set = 1, binding = 1) uniform sampler2D ImageT;
    layout (location = 0) in float2 iUV;
    layout (location = 0) out float4 oColor;

    void main() {

        oColor = applyBorderRadius(gl_FragCoord.xy, float4(1.0), push.borderRadius, push.size, push.transform);
    }
}