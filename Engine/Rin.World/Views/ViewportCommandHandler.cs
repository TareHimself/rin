using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;
using Rin.Shade;
using Rin.World.Graphics;
using Rin.World.Views.Shaders;

namespace Rin.World.Views;

internal partial class ViewportCommandHandler : ICommandHandlerWithPreAdd
{
    [GraphicsShader<ViewportShader>]
    private partial IGraphicsShader Shader { get; }

    private DrawViewportCommand[] _commands = [];

    private uint[] _outputImageIds = [];
    private uint[] _pushBufferIds = [];
    private IWorldCollectedData[] _renderContexts = [];

    public uint Id { get; set; }

    public void PreAdd(IGraphBuilder builder)
    {
        // c.Context was snapshotted on the collect thread in Viewport.CollectContent; Context.Write only
        // wires that snapshot into the graph, with no live world access here on the render thread.
        _renderContexts = new IWorldCollectedData[_commands.Length];
        for (var i = 0; i < _commands.Length; i++)
        {
            _commands[i].Context.Write(builder);
            _renderContexts[i] = _commands[i].Context;
        }
    }


    public void Init(ICommand[] commands)
    {
        _commands = new DrawViewportCommand[commands.Length];
        for (var i = 0; i < commands.Length; i++) _commands[i] = (DrawViewportCommand)commands[i];
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        _pushBufferIds = new uint[_commands.Length];
        for (var i = 0; i < _commands.Length; i++)
            _pushBufferIds[i] = config.CreateBuffer<ViewportPushData>(GraphBufferUsage.HostThenGraphics);

        _outputImageIds = new uint[_renderContexts.Length];
        for (var i = 0; i < _renderContexts.Length; i++)
            _outputImageIds[i] = config.ReadTexture(_renderContexts[i].GetOutputImageId(), ImageLayout.ShaderReadOnly);
    }

    public void Execute(IPassConfig passConfig,
        SurfaceContext surfaceContext, ICompiledGraph graph, IExecutionContext ctx)
    {
        if (Shader.Bind(ctx) is { } bindContext)
        {
            for (var i = 0; i < _commands.Length; i++)
            {
                var cmd = _commands[i];
                var outputImage = graph.GetImageOrException(_outputImageIds[i]);
                var pushBuffer = graph.GetBufferOrException(_pushBufferIds[i]);

                ctx.SetStencilCompareMask(cmd.StencilMask);
                pushBuffer.WriteSingle(
                    new ViewportPushData
                    {
                        Projection = surfaceContext.ProjectionMatrix,
                        Transform = cmd.Transform,
                        Size = cmd.DisplaySize,
                        OutputImage = outputImage
                    });
                bindContext
                    .Push(new ViewportShader.PushConstants { Data = pushBuffer.ToDeviceSpan<ViewportPushData>() })
                    .Draw(6);
            }
        }
    }
}

internal class DrawViewportCommand(
    IWorldCollectedData context,
    in Vector2 displaySize,
    in Matrix4x4 transform)
    : TCommand<MainPassConfig, ViewportCommandHandler>
{
    /// <summary>
    ///     Immutable world snapshot taken on the collect thread; render size is baked in.
    /// </summary>
    public IWorldCollectedData Context { get; } = context;

    /// <summary>
    ///     Size the composited quad is drawn at on the surface (the live pane size).
    /// </summary>
    public Vector2 DisplaySize { get; } = displaySize;

    public Matrix4x4 Transform { get; } = transform;
}
