namespace rsl.Test;

[TestClass]
public class TokenizerTests
{
    [TestMethod]
    public void Test()
    {
        var tokens = Tokenizer.Run("",
            @"
#include ""widget.ash""
#include ""utils.ash""

push(scalar)
{
    mat3 transform;
    float2 size;
    float4 borderRadius;
    float4 color;
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

        float4 pxColor = push.color;

        oColor = applyBorderRadius(gl_FragCoord.xy, pxColor, push.borderRadius, push.size, push.transform);
    }
}
"
            );
    }
}