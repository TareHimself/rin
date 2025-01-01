using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Graphics.Shaders.Rsl;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Commands;

namespace WidgetTest;

public class PrettyShaderDrawCommand(Mat3 transform,Vec2<float> size,bool hovered) : CustomCommand
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Data
    {
        public required Mat4 Projection;
        public required Vec2<float> ScreenSize;
        public required Mat3 Transform;
        public required Vec2<float> Size;
        public required float Time;
        public required Vec2<float> Center;
    }
    public override bool WillDraw => true;

    public override ulong MemoryNeeded => (ulong)Marshal.SizeOf<Data>();


    private readonly IGraphicsShader _prettyRslShader = SGraphicsModule.Get().GetShaderManager().GraphicsFromPath(Path.Join(SRuntime.ResourcesDirectory,"test","pretty.rsl"));

    public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBuffer? buffer = null)
    {
        frame.BeginMainPass();
        var cmd = frame.Raw.GetCommandBuffer();
        if (_prettyRslShader.Bind(cmd, true) && buffer != null)
        {
            var pushResource = _prettyRslShader.PushConstants.First().Value;
            var screenSize = frame.Surface.GetDrawSize().Cast<float>();
            var data = new Data()
            {
                Projection = frame.Projection,
                ScreenSize = screenSize,
                Transform = transform,
                Size = size,
                Time = (float)SRuntime.Get().GetTimeSeconds(),
                Center = hovered ?  frame.Surface.GetCursorPosition() : screenSize / 2.0f
            };
            buffer.Write(data);
            cmd.PushConstant(_prettyRslShader.GetPipelineLayout(), pushResource.Stages,buffer.GetAddress());
            cmd.Draw(6);
        }
    }
}