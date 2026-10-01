using System.Numerics;
using JetBrains.Annotations;
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
using Rin.Shade;
using Rin.World.Graphics;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Views.Shaders;
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
        // c.Context was snapshotted on the collect thread in Viewport.CollectContent; WriteSingle only
        // wires that snapshot into the graph — no live world access here on the render thread.
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

    private uint[][] _gBufferImageIds = [];
    private uint[] _lightsBufferIds = [];

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        _pushBufferIds = new uint[_commands.Length];
        for (var i = 0; i < _commands.Length; i++)
            _pushBufferIds[i] = config.CreateBuffer<ViewportPushData>(GraphBufferUsage.HostThenGraphics);

        _outputImageIds = new uint[_renderContexts.Length];
        _gBufferImageIds = new uint[_renderContexts.Length][];
        _lightsBufferIds = new uint[_renderContexts.Length];
        for (var i = 0; i < _renderContexts.Length; i++)
        {
            var context = _renderContexts[i];
            _outputImageIds[i] = config.ReadTexture(context.GetOutputImageId(), ImageLayout.ShaderReadOnly);

            var gBufferIds = new uint[4];
            for (var j = 0; j < gBufferIds.Length; j++)
            {
                var id = context.GetGBufferImageId(j);
                gBufferIds[j] = id > 0 ? config.ReadTexture(id, ImageLayout.ShaderReadOnly) : 0;
            }

            _gBufferImageIds[i] = gBufferIds;
            _lightsBufferIds[i] = config.CreateBuffer<LightInfo>(System.Math.Max(context.GetLights().Length, 1),
                GraphBufferUsage.HostThenGraphics);
        }
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
                var gBufferIds = _gBufferImageIds[i];
                var lights = _renderContexts[i].GetLights();
                var lightsBuffer = graph.GetBufferOrException(_lightsBufferIds[i]);
                if (lights.Length > 0) lightsBuffer.Write(lights);

                ctx.SetStencilCompareMask(cmd.StencilMask);
                pushBuffer.WriteSingle(
                    new ViewportPushData
                    {
                        Projection = surfaceContext.ProjectionMatrix,
                        Transform = cmd.Transform,
                        Size = cmd.DisplaySize,
                        OutputImage = outputImage,
                        GBuffer = new GBufferHandles
                        {
                            GBuffer0 = gBufferIds[0] > 0 ? graph.GetImageOrException(gBufferIds[0]) : ResourceHandle.InvalidTexture,
                            GBuffer1 = gBufferIds[1] > 0 ? graph.GetImageOrException(gBufferIds[1]) : ResourceHandle.InvalidTexture,
                            GBuffer2 = gBufferIds[2] > 0 ? graph.GetImageOrException(gBufferIds[2]) : ResourceHandle.InvalidTexture,
                            GBuffer3 = gBufferIds[3] > 0 ? graph.GetImageOrException(gBufferIds[3]) : ResourceHandle.InvalidTexture
                        },
                        Lights = new BufferRef<LightInfo>(lightsBuffer.GetAddress()),
                        LightCount = lights.Length,
                        Channel = cmd.Channel
                    });
                bindContext
                    .Push(new ViewportShader.PushConstants { Data = new BufferRef<ViewportPushData>(pushBuffer.GetAddress()) })
                    .Draw(6);
            }
        }
    }
}

internal class DrawViewportCommand(
    IWorldCollectedData context,
    in Vector2 displaySize,
    in Matrix4x4 transform,
    ViewportChannel channel)
    : TCommand<MainPassConfig, ViewportCommandHandler>
{
    /// <summary>Immutable world snapshot taken on the collect thread; render size is baked in.</summary>
    public IWorldCollectedData Context { get; } = context;

    /// <summary>Count the composited quad is drawn at on the surface (the live pane size).</summary>
    public Vector2 DisplaySize { get; } = displaySize;

    public Matrix4x4 Transform { get; } = transform;

    public ViewportChannel Channel { get; } = channel;
}

public class Viewport : ContentView
{
    /// <summary>Frames the content size must hold steady before the render target follows it.</summary>
    public static int SettleFrames = 5;

    private readonly CameraComponent _targetCamera;
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
        var renderSystem = _targetCamera.Owner!.World!.RenderSystem;
        var context = renderSystem.Snapshot(_targetCamera, GetRenderSize().ToExtent());
        commands.Add(new DrawViewportCommand(context, GetContentSize(), transform, _channel));
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
    }
}