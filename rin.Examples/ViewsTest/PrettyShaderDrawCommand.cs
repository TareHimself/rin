using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Rin.Engine.Core;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Commands;
using Utils = Rin.Engine.Core.Utils;

namespace rin.Examples.ViewsTest;

public class PrettyShaderDrawCommand(Matrix4x4 transform, Vector2 size, bool hovered) : CustomCommand
{
    public static string ShaderSource = @"
#include ""Engine/Shaders/utils.slang""

struct Data {
    float4x4 projection;
    float2 screenSize;
    float3x3 transform;
    float2 size;
    float time;
    float2 cursor;
}

struct PushConstants
{
    Data *data;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

[shader(""vertex"")]
float4 vertex(int instanceId: SV_InstanceID, int vertexId: SV_VertexID, out float2 oUV: UV)
    : SV_Position
{
    float4 position;
    generateRectVertex(push.data->size, push.data->projection, push.data->transform, vertexId, position, oUV);
    return position;
}

// https://iquilezles.org/articles/palettes/
float3 palette(float t) {
    float3 a = float3(0.5, 0.5, 0.5);
    float3 b = float3(0.5, 0.5, 0.5);
    float3 c = float3(1.0, 1.0, 1.0);
    float3 d = float3(0.263, 0.416, 0.557);
    // float3 a = PushConstants.data1.rgb;
    // float3 b = PushConstants.data2.rgb;
    // float3 c = PushConstants.data3.rgb;
    // float3 d = PushConstants.data3.rgb;

    return a + b * cos(6.28318 * (c * t + d));
}

// https://www.shadertoy.com/view/mtyGWy
[shader(""fragment"")]
float4 fragment(in float2 iUV: UV, float2 coordinate: SV_Position)
    : SV_Target
{
    var screenSize = push.data->screenSize;
    var center = screenSize / 2.0;
    float2 position = coordinate / screenSize;//((coordinate + 1.0) / 2.0);
    //return float4(position,0.0, 1.0);
    float2 fragCoord = coordinate;
    int2 iResolution = int2((int)push.data->size.x, (int)push.data->size.y);
    float iTime = push.data->time;

    float2 cursor = push.data->cursor;
    cursor = (screenSize / 2.0) - cursor;
    float2 uv = ((coordinate + cursor) * 2.0 - screenSize.xy) / screenSize.y;
    float2 screen = push.data->screenSize;
    //uv = uv - float2(mapRangeUnClamped(cursor.x, 0.0, size.x, 0.0, 1.0), mapRangeUnClamped(cursor.y, 0.0, size.y, 0.0, 1.0));
    float2 uv0 = uv;

    float3 finalColor = float3(0.0);

    for (float i = 0.0; i < 4.0; i++) {
        uv = fract(uv * lerp(1.5,2.0,((sin(iTime) + 1.0) / 2.0))) - 0.5;

        float d = length(uv) * exp(length(uv0) * -1.0);

        float3 col = palette(length(uv0) + i * 0.4 + iTime * 0.4);

        d = sin(d * 8.0 + iTime) / 8.0;
        d = abs(d);

        d = pow(0.01 / d, 1.2);

        finalColor += col * d;
    }

    return float4(finalColor, 1.0);
}
";

    private static bool _test = HandleLoad();

    private static readonly string ShaderPath =
        SEngine.Get().Temp.AddStream(() => new MemoryStream(Encoding.UTF8.GetBytes(ShaderSource)));

    //private readonly IGraphicsShader _prettyShader = SGraphicsModule.Get().MakeGraphics( $"fs/{Path.Join(SEngine.AssetsDirectory,"test","pretty.slang").Replace('\\', '/' )}");
    private readonly IGraphicsShader
        _prettyShader =
            SGraphicsModule.Get()
                .MakeGraphics(
                    ShaderPath); //($"fs/{Path.Join(SEngine.AssetsDirectory,"test","pretty.slang").Replace('\\', '/' )}");

    public override bool WillDraw()
    {
        return false;
    }

    public override ulong GetRequiredMemory()
    {
        return Utils.ByteSizeOf<Data>();
    }

    public static bool HandleLoad()
    {
        Console.WriteLine("Assembly loaded");

        return false;
    }

    public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBufferView? view = null)
    {
        frame.BeginMainPass();
        var cmd = frame.Raw.GetCommandBuffer();
        if (_prettyShader.Bind(cmd, true) && view != null)
        {
            var pushResource = _prettyShader.PushConstants.First().Value;
            var screenSize = frame.Surface.GetSize();
            var data = new Data
            {
                Projection = frame.Projection,
                ScreenSize = screenSize,
                Transform = transform,
                Size = size,
                Time = SEngine.Get().GetTimeSeconds(),
                Center = hovered ? frame.Surface.GetCursorPosition() : screenSize / 2.0f
            };
            view.Write(data);
            cmd.PushConstant(_prettyShader.GetPipelineLayout(), pushResource.Stages, view.GetAddress());
            cmd.Draw(6);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Data
    {
        public required Matrix4x4 Projection;
        public required Vector2 ScreenSize;
        public required Matrix4x4 Transform;
        public required Vector2 Size;
        public required float Time;
        public required Vector2 Center;
    }
}