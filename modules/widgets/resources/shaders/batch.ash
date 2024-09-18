#include "utils.ash"
#include "global.ash"

struct QuadRenderInfo
{
    int textureId;
    float4 color;
    float4 borderRadius;
    float2 size;
    mat3 transform;
};


layout(set = 1,binding = 0, scalar) uniform batch_info {
    float4 viewport;
    mat4 projection;
    QuadRenderInfo quads[64];
};

@Vertex{

    layout(location = 0) out float2 oUV;
    layout(location = 1) out int oQuadIndex;

    void main(){
        int index = gl_VertexIndex;
        int vertexIndex = int(mod(index, 6));
        int quadIndex = int(floor(index / 6));
        QuadRenderInfo renderInfo = batch_info.quads[quadIndex];
        generateRectVertex(renderInfo.size, batch_info.projection, renderInfo.transform, vertexIndex, gl_Position, oUV);
        oQuadIndex = quadIndex;
    }
}


@Fragment{
    layout (location = 0) in float2 iUV;
    layout (location = 1,$flat) in int iQuadIndex;
    layout (location = 0) out float4 oColor;

    void main(){
        QuadRenderInfo renderInfo = batch_info.quads[iQuadIndex];
        float4 pxColor = renderInfo.color;

        oColor = applyBorderRadius(gl_FragCoord.xy, pxColor, renderInfo.borderRadius, renderInfo.size, renderInfo.transform);
    }
}