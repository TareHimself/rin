import rin.utils;
import rin.sd;


float dist_to_line(float2 a, float2 b, float2 p)
{
    var dir = b - a;
    var norm = float2(-dir.y, dir.x);
    return dot(normalize(norm), a - p);
}

float calc_t(float2 a, float2 b, float2 p)
{
    var dir = b - a;
    var t = dot(p - a, dir) / dot(dir, dir);
    return clamp(t, 0.0, 1.0);
}

float dist_to_bezier2(float2 p0, float2 p1, float2 p2, float2 p)
{
    var t = calc_t(p0, p2, p);
    var q0 = lerp(p0, p1, t);
    var q1 = lerp(p1, p2, t);
    return dist_to_line(q0, q1, p);
}

struct Bezier {
    float2 begin;
    float2 end;
    float2 control;
}

struct PushConstants
{
    float4x4 projection;
    float3x3 transform;
    float2 size;
    int bezierCount;
    Bezier *beziers;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

struct VSIn
{
    int vertexId : SV_VertexID;
}

struct VSOut
{
    float2 uv : UV;
    float4 position : SV_Position;
}

struct FSIn
{
    float2 uv : UV;
    float2 coordinate : SV_Position;
}

[shader("vertex")]
VSOut vertex(VSIn input)

{
    VSOut output;
    generateRectVertex(push.size, push.projection, push.transform, input.vertexId, output.position, output.uv);
    return output;
}

#define UDIST_BIAS 0.001

[shader("fragment")]
float4 fragment(FSIn input)
    : SV_Target
{
    float d = 0.0f;
    float ud = 1.0f/0.0f;

    for (var i = 0; i < push.bezierCount; i++) {
        var bezier = push.beziers[i];
        var t_ = calc_t(bezier.begin,bezier.end,input.coordinate);
        var ud_ = distance(lerp(bezier.begin, bezier.end, t_), input.coordinate);
        // if (ud_ <= ud + UDIST_BIAS) {

        //     var d_ = max(d, dist_to_bezier2(bezier.end, bezier.control, bezier.begin, input.coordinate));

        //     if (ud_ >= ud - UDIST_BIAS) {
        //         var prev = push.beziers[i - 1];
        //         var prevd = dist_to_line(bezier.begin, bezier.end, prev.begin);
        //         d = max(d_, d);//lerp(min(d_, d), max(d_, d), 1);//step(prevd, 0.0));
        //     }
        //     else {
        //         d = d_;
        //     }

        //     ud = min(ud_,ud);
        // }
        // if (abs(ud - ud_) <= UDIST_BIAS)
        // {
        //     var d_ = max(d, dist_to_bezier2(bezier.end, bezier.control, bezier.begin, input.coordinate));
        //     var prev = push.beziers[i - 1];
        //     var prevd = dist_to_line(bezier.begin, bezier.end, prev.begin);
        //     d = max(d_, d);//lerp(min(d_, d), max(d_, d), 1);//step(prevd, 0.0));
        //     //d = max(d, dist_to_bezier2(bezier.end, bezier.control, bezier.begin, input.coordinate));
        // }
        // else
        // if(ud_ < ud){
        //     ud = ud_;
        //     d = dist_to_bezier2(bezier.end, bezier.control, bezier.begin, input.coordinate);
        // }
    }

    float thickness = 2.0f; // Assuming thickness is stored in data3.z
    float smoothedAlpha = smoothstep(thickness * 0.5, thickness * 0.5 + 3.0, d);
    // var d = dist_to_bezier2(push.beziers[0].end, push.beziers[0].control, push.beziers[0].begin, input.coordinate);//sdBezier(input.coordinate, push.beziers[0].begin, push.beziers[0].end, push.beziers[0].control);
    // //var d = sdBezier(input.coordinate,)
    // float thickness = 1.0f; // Assuming thickness is stored in data3.z
    // float smoothedAlpha = smoothstep(thickness * 0.5, thickness * 0.5 + 3.0, d);
    
    // if (d > 0.0f) {
    //     discard;
    // }
    return float4(float3(1.0f), smoothedAlpha);
}