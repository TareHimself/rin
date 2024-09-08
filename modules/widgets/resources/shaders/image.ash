
#include "rect.ash"

@Fragment {
    layout(set = 1, binding = 1) uniform sampler2D ImageT;
    layout (location = 0) in float2 iUV;
    layout (location = 0) out float4 oColor;

    layout(set = 1, binding = 0, scalar) uniform opts {
        float4 tint;// the image tint
        int bHasTexture;// check if we have a texture
        float4 borderRadius;
    };

    void main() {
        float4 pxColor = opts.tint;

        if (opts.bHasTexture == 1){
            pxColor = pxColor * texture(ImageT, iUV);
        }

        oColor = applyBorderRadius(gl_FragCoord.xy, pxColor, opts.borderRadius, push.size, push.transform);
    }
}