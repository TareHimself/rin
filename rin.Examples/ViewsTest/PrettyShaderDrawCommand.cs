﻿using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Engine.Core;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Commands;
using Utils = Rin.Engine.Core.Utils;

namespace rin.Examples.ViewsTest;

public class PrettyShaderDrawCommand(Mat3 transform,Vector2 size,bool hovered) : CustomCommand
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Data
    {
        public required Mat4 Projection;
        public required Vector2 ScreenSize;
        public required Mat3 Transform;
        public required Vector2 Size;
        public required float Time;
        public required Vector2 Center;
    }

    public override bool WillDraw() => false;

    public override ulong GetRequiredMemory() => Utils.ByteSizeOf<Data>();

    private readonly IGraphicsShader _prettyShader = SGraphicsModule.Get().MakeGraphics(Path.Join(SEngine.AssetsDirectory,"test","pretty.slang"));

    public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBufferView? view = null)
    {
        frame.BeginMainPass();
        var cmd = frame.Raw.GetCommandBuffer();
        if (_prettyShader.Bind(cmd, true) && view != null)
        {
            var pushResource = _prettyShader.PushConstants.First().Value;
            var screenSize = frame.Surface.GetSize();
            var data = new Data()
            {
                Projection = frame.Projection,
                ScreenSize = screenSize,
                Transform = transform,
                Size = size,
                Time = (float)SEngine.Get().GetTimeSeconds(),
                Center = hovered ?  frame.Surface.GetCursorPosition() : screenSize / 2.0f
            };
            view.Write(data);
            cmd.PushConstant(_prettyShader.GetPipelineLayout(), pushResource.Stages,view.GetAddress());
            cmd.Draw(6);
        }
    }
}