#include "utils.rsl"
#include "widget.rsl"

push(scalar)
{
    mat3 transform;
    float2 size;
};

@Vertex{
    layout(location = 0) out float2 oUV;

    void main()
    {
        generateRectVertex(push.size, ui.projection, push.transform, gl_VertexIndex, gl_Position, oUV);
    }
}