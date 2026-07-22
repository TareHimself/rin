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
    Emissive
}

internal class ViewportCommandHandler : ICommandHandlerWithPreAdd
{
    private readonly IGraphicsShader _shader = IGraphicsModule.Get()
        .MakeGraphics("World/Shaders/viewport.slang");

    private DrawViewportCommand[] _commands = [];

    private uint[] _outputImageIds = [];
    private uint[] _pushBufferIds = [];
    private IWorldRenderContext[] _renderContexts = [];

    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void PreAdd(IGraphBuilder builder)
    {
        _renderContexts = _commands.Select(c => c.Render.Collect(builder, c.Camera, c.Size.ToExtent())).ToArray();
    }


    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<DrawViewportCommand>().ToArray();
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        _pushBufferIds = _commands
            .Select(c => config.CreateBuffer<PushData>(GraphBufferUsage.HostThenGraphics))
            .ToArray();
        _outputImageIds = _renderContexts
            .Select(c => config.ReadTexture(c.GetOutputImageId(), ImageLayout.ShaderReadOnly))
            .ToArray();
    }

    public void Execute(IPassConfig passConfig,
        SurfaceContext surfaceContext, ICompiledGraph graph, IExecutionContext ctx)
    {
        if (_shader.Bind(ctx) is { } bindContext)
        {
            var outputImages = _outputImageIds.Select(graph.GetImageOrException);
            var pushBuffers = _pushBufferIds.Select(graph.GetBufferOrException);

            foreach (var (cmd, outputImage, pushBuffer) in _commands.Zip(outputImages, pushBuffers))
            {
                ctx.SetStencilCompareMask(cmd.StencilMask);
                pushBuffer.Write(
                    new PushData
                    {
                        Projection = surfaceContext.ProjectionMatrix,
                        Transform = cmd.Transform,
                        Size = cmd.Size,
                        OutputImage = outputImage
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
    }
}

internal class DrawViewportCommand(
    CameraComponent camera,
    in Vector2 extent,
    in Matrix4x4 transform,
    IWorldRenderer renderer)
    : TCommand<MainPassConfig, ViewportCommandHandler>
{
    public Vector2 Size { get; } = extent;
    public CameraComponent Camera { get; } = camera;
    public Matrix4x4 Transform { get; } = transform;

    public IWorldRenderer Render { get; } = renderer;
}

public class Viewport : ContentView
{
    private readonly CameraComponent _targetCamera;
    private readonly DefaultWorldRenderer _worldRenderer = new();
    private bool _captureMouse;
    private ViewportChannel _channel = ViewportChannel.Scene;
    private bool _ignoreNextMove;
    private Vector2 _mousePosition;

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
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace;
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
                currentIdx = (currentIdx + 1) % 5;
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
        commands.Add(new DrawViewportCommand(_targetCamera, GetContentSize(), transform, _worldRenderer));
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