using System.Numerics;
using Examples.AudioPlayer.Shaders;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Shade;

namespace Examples.AudioPlayer.Views;

public partial class RadialSpectrumCommandHandler : ICommandHandler
{
    private uint _bandBuffer;
    private int _bandEntries;
    private RadialSpectrumCommand[] _commands = [];
    private uint _dataBuffer;

    [GraphicsShader<SpectrumShader>]
    private partial IGraphicsShader Shader { get; }

    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<RadialSpectrumCommand>().ToArray();
        _bandEntries = 0;
        foreach (var command in _commands) _bandEntries += command.PackedBands.Length;
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        _dataBuffer = config.CreateBuffer<SpectrumData>(_commands.Length, GraphBufferUsage.HostThenGraphics);
        _bandBuffer = config.CreateBuffer<Vector4>(_bandEntries, GraphBufferUsage.HostThenGraphics);
    }

    public void Execute(IPassConfig passConfig, SurfaceContext surfaceContext, ICompiledGraph graph,
        IExecutionContext ctx)
    {
        if (Shader.Bind(ctx) is not { } bindContext) return;

        var dataBuffer = graph.GetBufferOrException(_dataBuffer);
        var bandBuffer = graph.GetBufferOrException(_bandBuffer);
        var time = IApplication.Get().TimeSeconds;
        ulong bandOffset = 0;

        for (var i = 0; i < _commands.Length; i++)
        {
            var command = _commands[i];
            var bandBytes = Utils.ByteSizeOf<Vector4>(command.PackedBands.Length);
            var dataView = dataBuffer.GetView<SpectrumData>(Utils.ByteSizeOf<SpectrumData>(i));
            var bandView = bandBuffer.GetView(bandOffset, bandBytes);

            dataView.WriteSingle(new SpectrumData
            {
                Projection = surfaceContext.ProjectionMatrix,
                Transform = command.Transform,
                Size = command.Size,
                InnerRadius = command.InnerRadius,
                OuterRadius = command.OuterRadius,
                Pulse = command.Pulse,
                Time = time,
                Mode = command.Mode,
                BandCount = command.BandCount,
                PrimaryColor = command.Palette.Primary,
                SecondaryColor = command.Palette.Secondary
            });
            bandView.Write(new ReadOnlySpan<Vector4>(command.PackedBands));

            ctx.SetStencilCompareMask(command.StencilMask);
            bindContext
                .Push(new SpectrumShader.PushConstants
                {
                    Data = new BufferRef<SpectrumData>(dataView.GetAddress()),
                    Bands = new BufferRef<Vector4>(bandView.GetAddress())
                })
                .Draw(6);

            bandOffset += bandBytes;
        }
    }
}
