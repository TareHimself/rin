
@Vertex {
    layout(location = 0) out float2 oUV;


    void main(){
        float2 normPt1 = float2(-1.0);
        float2 normPt2 = float2(1.0);
        float2 size = normPt2 - normPt1;
        float2 midpoint = normPt1 + (size / 2.0);

        // float2 vertex[] = { float2(-0.5),float2(0.5,-0.5),float2(0.5),float2(-0.5),float2(0.5),float2(-0.5,0.5) };
        float2 vertex[] = { float2(-0.5), float2(0.5, -0.5), float2(0.5), float2(-0.5), float2(0.5), float2(-0.5, 0.5) };

        gl_Position = float4(midpoint + (size * vertex[gl_VertexIndex]), 0, 1);

        // gl_Position = float4(vertex, 0.0, 1.0);

        oUV = vertex[gl_VertexIndex] + 0.5;
    }
}

@Fragment {
    #include "pbr.ash"

    layout(set = 1, binding = 0) uniform sampler2D TColor;
    layout(set = 1, binding = 1) uniform sampler2D TLocation;
    layout(set = 1, binding = 2) uniform sampler2D TNormal;
    layout(set = 1, binding = 3) uniform sampler2D TRoughMetallicSpecular;
    layout(set = 1, binding = 5) uniform sampler2D TEmissive;

    layout(location = 0) in float2 iUV;
    layout(location = 0) out float4 oColor;
    //float4 computePixelColor(float3 sceneLocation,float3 color,float3 normal,float roughness,float metallic,float3 emissive)

    void main(){

        float4 color = texture(TColor, iUV);
        

        if(color.w < 0.5){
            discard;
        }

        float3 sceneLocation = texture(TLocation, iUV).xyz;
        float3 normal = texture(TNormal, iUV).xyz;
        float3 roughMetallicSpecular = texture(TRoughMetallicSpecular, iUV).xyz;
        float3 emissive = texture(TEmissive, iUV).xyz;
        oColor = computePixelColor(sceneLocation,color.xyz,normal,float3(1.0,0.0,0.1),emissive);
       // oColor = computePixelColor(sceneLocation,color.xyz,normal,roughMetallicSpecular,emissive);
    }
}