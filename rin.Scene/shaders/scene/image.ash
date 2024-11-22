
#include "rect.ash"

@Fragment {
    layout(set = 1, binding = 0) uniform sampler2D ImageT;
    layout (location = 0) in float2 iUV;
    layout (location = 0) out float4 oColor;

    void main() {
        oColor = texture(ImageT, iUV);
    }
}