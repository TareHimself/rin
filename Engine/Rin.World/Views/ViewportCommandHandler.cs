using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;
using Rin.Core.Views.Graphics.Quads;
using Rin.World.Components;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using CommandList = Rin.Core.Views.Graphics.CommandList;
using ICommand = Rin.Core.Views.Graphics.Commands.ICommand;

namespace Rin.World.Views;

public enum ViewportChannel
{
    Scene,
    Color,
    Location,
    Normal,
    RoughnessMetallicSpecular,
    Emissive,
    Radiance
}

internal class ViewportCommandHandler : ICommandHandlerWithPreAdd
{
    private readonly IGraphicsShader _shader = IGraphicsModule.Get()
        .MakeGraphics("Shaders/World/viewport.slang");

    private DrawViewportCommand[] _commands = [];

    private uint[] _outputImageIds = [];
    private uint[] _pushBufferIds = [];
    private IWorldRenderContext[] _renderContexts = [];

    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void PreAdd(IGraphBuilder builder)
    {
        // c.Context was snapshotted on the collect thread in Viewport.CollectContent; Build only
        // wires that snapshot into the graph — no live world access here on the render thread.
        _renderContexts = _commands.Select(c =>
        {
            c.Render.Build(builder, c.Context);
            return c.Context;
        }).ToArray();
    }


    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<DrawViewportCommand>().ToArray();
    }

    private uint[][] _gBufferImageIds = [];
    private uint[] _lightsBufferIds = [];

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        _pushBufferIds = _commands
            .Select(c => config.CreateBuffer<PushData>(GraphBufferUsage.HostThenGraphics))
            .ToArray();
        _outputImageIds = _renderContexts
            .Select(c => config.ReadTexture(c.GetOutputImageId(), ImageLayout.ShaderReadOnly))
            .ToArray();
        _gBufferImageIds = _renderContexts
            .Select(c => Enumerable.Range(0, 4)
                .Select(i => c.GetGBufferImageId(i))
                .Select(id => id > 0 ? config.ReadTexture(id, ImageLayout.ShaderReadOnly) : 0)
                .ToArray())
            .ToArray();
        _lightsBufferIds = _renderContexts
            .Select(c => config.CreateBuffer<LightInfo>(System.Math.Max(c.GetLights().Length, 1),
                GraphBufferUsage.HostThenGraphics))
            .ToArray();
    }

    public void Execute(IPassConfig passConfig,
        SurfaceContext surfaceContext, ICompiledGraph graph, IExecutionContext ctx)
    {
        if (_shader.Bind(ctx) is { } bindContext)
        {
            var outputImages = _outputImageIds.Select(graph.GetImageOrException).ToArray();
            var pushBuffers = _pushBufferIds.Select(graph.GetBufferOrException).ToArray();
            var lightsBuffers = _lightsBufferIds.Select(graph.GetBufferOrException).ToArray();

            for (var i = 0; i < _commands.Length; i++)
            {
                var cmd = _commands[i];
                var outputImage = outputImages[i];
                var pushBuffer = pushBuffers[i];
                var gBufferIds = _gBufferImageIds[i];
                var lights = _renderContexts[i].GetLights();
                var lightsBuffer = lightsBuffers[i];
                if (lights.Length > 0) lightsBuffer.Write(lights);

                ctx.SetStencilCompareMask(cmd.StencilMask);
                pushBuffer.Write(
                    new PushData
                    {
                        Projection = surfaceContext.ProjectionMatrix,
                        Transform = cmd.Transform,
                        Size = cmd.DisplaySize,
                        OutputImage = outputImage,
                        GBuffer0 = gBufferIds[0] > 0 ? graph.GetImageOrException(gBufferIds[0]) : ResourceHandle.InvalidTexture,
                        GBuffer1 = gBufferIds[1] > 0 ? graph.GetImageOrException(gBufferIds[1]) : ResourceHandle.InvalidTexture,
                        GBuffer2 = gBufferIds[2] > 0 ? graph.GetImageOrException(gBufferIds[2]) : ResourceHandle.InvalidTexture,
                        GBuffer3 = gBufferIds[3] > 0 ? graph.GetImageOrException(gBufferIds[3]) : ResourceHandle.InvalidTexture,
                        LightsBuffer = lightsBuffer.GetAddress(),
                        LightCount = lights.Length,
                        Channel = (int)cmd.Channel
                    });
                bindContext
                    .Push(pushBuffer.GetAddress())
                    .Draw(6);
            }
        }
    }

    private struct PushData
    {
        public required Matrix4x4 Projection;
        public required Matrix4x4 Transform;
        public required Vector2 Size;
        public required ResourceHandle OutputImage;
        public required ResourceHandle GBuffer0;
        public required ResourceHandle GBuffer1;
        public required ResourceHandle GBuffer2;
        public required ResourceHandle GBuffer3;
        public required ulong LightsBuffer;
        public required int LightCount;
        public required int Channel;
    }
}

internal class DrawViewportCommand(
    IWorldRenderContext context,
    in Vector2 displaySize,
    in Matrix4x4 transform,
    IWorldRenderer renderer,
    ViewportChannel channel)
    : TCommand<MainPassConfig, ViewportCommandHandler>
{
    /// <summary>Immutable world snapshot taken on the collect thread; render size is baked in.</summary>
    public IWorldRenderContext Context { get; } = context;

    /// <summary>Size the composited quad is drawn at on the surface (the live pane size).</summary>
    public Vector2 DisplaySize { get; } = displaySize;

    public Matrix4x4 Transform { get; } = transform;

    public IWorldRenderer Render { get; } = renderer;
    public ViewportChannel Channel { get; } = channel;
}

public class Viewport : ContentView
{
    /// <summary>Frames the content size must hold steady before the render target follows it.</summary>
    public static int SettleFrames = 5;

    private readonly CameraComponent _targetCamera;
    private readonly DefaultWorldRenderer _worldRenderer = new();
    private bool _captureMouse;
    private ViewportChannel _channel = ViewportChannel.Scene;
    private bool _ignoreNextMove;
    private Vector2 _mousePosition;

    // Render-target size is debounced: it only follows the content size once that size has
    // settled, so a live resize (splitter / window drag) stretches the last render instead of
    // reallocating the whole G-buffer set every frame.
    private Vector2 _renderSize;
    private Vector2 _lastContentSize;
    private int _settleFrames;

    public Viewport(CameraComponent camera)
    {
        _targetCamera = camera;
        GetModeText();
    }

    public override bool IsFocusable => true;

    protected Vector2 GetAbsoluteCenter()
    {
        return (GetContentSize() / 2.0f).Transform(ComputeAbsoluteContentTransform());
    }

    private string GetModeText()
    {
        return _channel switch
        {
            ViewportChannel.Scene => "Default",
            ViewportChannel.Color => "Color",
            ViewportChannel.Location => "Location",
            ViewportChannel.Normal => "Normal",
            ViewportChannel.RoughnessMetallicSpecular => "Roughness Metallic Specular",
            ViewportChannel.Emissive => "Emissive",
            ViewportChannel.Radiance => "Radiance",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        var content = GetContentSize();
        if (content is { X: > 0f, Y: > 0f })
        {
            if (content == _lastContentSize)
            {
                if (_settleFrames < SettleFrames) _settleFrames++;
                if (_settleFrames >= SettleFrames) _renderSize = content;
            }
            else
            {
                _settleFrames = 0;
            }

            _lastContentSize = content;
            if (_renderSize is not { X: > 0f, Y: > 0f }) _renderSize = content;
        }
    }

    /// <summary>Debounced size the world is actually rendered at (see <see cref="_renderSize" />).</summary>
    protected Vector2 GetRenderSize()
    {
        var content = GetContentSize();
        return _renderSize is { X: > 0f, Y: > 0f } ? _renderSize : content;
    }

    // public override void SetSize(Vector2<float> size)
    // {
    //     base.SetSize(size);
    //     TargetScene.Drawer?.Resize(new Vector2<uint>((uint)Math.Ceiling(size.Width),(uint)Math.Ceiling(size.Height)));
    // }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        if (_captureMouse)
        {
            _captureMouse = false;
            _ignoreNextMove = false;
            if (IsFocused) e.Surface.ClearFocus();
        }

        base.OnCursorUp(e);
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        switch (e.Button)
        {
            case CursorButton.One:
            {
                var currentIdx = (int)_channel;
                currentIdx = (currentIdx + 1) % 7;
                _channel = (ViewportChannel)currentIdx;
                GetModeText();
                e.Target = this;
                break;
            }
            case CursorButton.Two:
                _captureMouse = true;
                _ignoreNextMove = true;
                _mousePosition = GetAbsoluteCenter();
                e.Surface.SetCursorPosition(_mousePosition);
                e.Surface.RequestFocus(this);
                e.Target = this;
                break;
        }
    }

    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
        if (_captureMouse && !_ignoreNextMove)
        {
            var delta = e.Position - _mousePosition;
            if (!(float.Abs(delta.X) > 0) && !(float.Abs(delta.Y) > 0)) return;

            OnMouseDelta(delta);

            _mousePosition = GetAbsoluteCenter();
            _ignoreNextMove = true;
            e.Surface.SetCursorPosition(_mousePosition);
            e.Target = this;
            return;
        }

        if (_ignoreNextMove)
        {
            _ignoreNextMove = false;
            _mousePosition = e.Position;
        }

        base.OnCursorMove(e, transform);
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2();
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        // Runs on the collect (main) thread, inside the render barrier — safe to walk the World.
        var context = _worldRenderer.Snapshot(_targetCamera, GetRenderSize().ToExtent());
        commands.Add(new DrawViewportCommand(context, GetContentSize(), transform, _worldRenderer, _channel));
        commands.AddText(transform, "Noto Sans", GetModeText());
    }


    protected virtual void OnMouseDelta(Vector2 delta)
    {
        // //Console.WriteLine($"Mouse Moved X : {delta.X} , Y : {delta.Y} ");
        // var viewTarget = _targetCamera?.RootComponent;
        // if (viewTarget == null) return;//.ApplyYaw(delta.X).ApplyPitch(delta.Y)
        // viewTarget.SetRelativeRotation(viewTarget.GetRelativeRotation().Delta(pitch: delta.Y, yaw: delta.X));
    }

    public override void Dispose()
    {
        base.Dispose();
        _worldRenderer.Dispose();
    }
}