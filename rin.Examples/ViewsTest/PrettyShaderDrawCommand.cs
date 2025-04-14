using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Rin.Engine;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Commands;
using Utils = Rin.Engine.Utils;

namespace rin.Examples.ViewsTest;

public class PrettyShaderDrawCommand(Matrix4x4 transform, Vector2 size, bool hovered) : CustomCommand
{
    
    //private readonly IGraphicsShader _prettyShader = SGraphicsModule.Get().MakeGraphics( $"fs/{Path.Join(SEngine.AssetsDirectory,"test","pretty.slang").Replace('\\', '/' )}");
    private readonly IGraphicsShader
        _prettyShader =
            SGraphicsModule.Get()
                .MakeGraphics($"fs/{Path.Join(SEngine.AssetsDirectory,"test","pretty.slang").Replace('\\', '/' )}");

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