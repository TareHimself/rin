using System.Diagnostics;
using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Math;
using Rin.Engine.Views;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Commands;
using Rin.Engine.Views.Graphics.Quads;
using Rin.Engine.World.Components;
using Rin.Engine.World.Graphics;
using Rin.Engine.World.Graphics.Default;
using CommandList = Rin.Engine.Views.Graphics.CommandList;

namespace Rin.Engine.World.Views;

public enum ViewportChannel
{
    Scene,
    Color,
    Location,
    Normal,
    RoughnessMetallicSpecular,
    Emissive
}

internal class ViewPortPass(SharedPassContext info, DrawViewportCommand command) : IViewsPass, IWithPreAdd
{
    private readonly Extent2D _renderExtent = command.Size.ToExtent();

    private readonly IShader _shader = SGraphicsModule.Get()
        .MakeGraphics("World/Shaders/viewport.slang");

    private uint _outputImageId;
    private uint _pushBufferId;
    private IWorldRenderContext? _renderContext;
    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void Configure(IGraphConfig config)
    {
        Debug.Assert(_renderContext != null);

        config.WriteImage(info.MainImageId, ImageLayout.ColorAttachment);
        config.ReadImage(info.StencilImageId, ImageLayout.StencilAttachment);
        _pushBufferId = config.CreateBuffer<PushData>(GraphBufferUsage.HostThenGraphics);
        _outputImageId = config.ReadImage(_renderContext.GetOutputImageId(), ImageLayout.ShaderReadOnly);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (_shader.Bind(ctx))
        {
            var outputImage = graph.GetImage(_outputImageId);
            var mainImage = graph.GetImageOrException(info.MainImageId);
            var stencilImage = graph.GetImageOrException(info.StencilImageId);
            var pushBuffer = graph.GetBufferOrException(_pushBufferId);
            pushBuffer.Write(
                new PushData
                {
                    Projection = info.ProjectionMatrix,
                    Transform = command.Transform,
                    Size = command.Size,
                    OutputImage = outputImage.BindlessHandle
                });

            ctx
                .BeginRendering(_renderExtent, [mainImage], stencilAttachment: stencilImage)
                .DisableFaceCulling()
                .StencilCompareOnly()
                .SetStencilCompareMask(command.StencilMask);
            _shader.Push(ctx, pushBuffer.GetAddress());
            ctx.Draw(6)
                .EndRendering();
        }
    }

    public static IViewsPass Create(PassCreateInfo info)
    {
        var cmd = info.Commands.First() as DrawViewportCommand ?? throw new NullReferenceException();
        return new ViewPortPass(info.Context, cmd);
    }

    public void PreAdd(IGraphBuilder builder)
    {
        _renderContext = command.Render.Collect(builder, command.Camera, _renderExtent);
    }

    private struct PushData
    {
        public required Matrix4x4 Projection;
        public required Matrix4x4 Transform;
        public required Vector2 Size;
        public required ImageHandle OutputImage;
    }
}

internal class DrawViewportCommand(
    CameraComponent camera,
    in Vector2 extent,
    in Matrix4x4 transform,
    IWorldRenderer renderer)
    : TCommand<ViewPortPass>
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
        return (GetContentSize() / 2.0f).Transform(ComputeAbsoluteTransform());
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

    public override bool OnCursorDown(CursorDownSurfaceEvent e)
    {
        switch (e.Button)
        {
            case CursorButton.One:
            {
                var currentIdx = (int)_channel;
                currentIdx = (currentIdx + 1) % 5;
                _channel = (ViewportChannel)currentIdx;
                GetModeText();
                return true;
            }
            case CursorButton.Two:
                _captureMouse = true;
                _ignoreNextMove = true;
                _mousePosition = GetAbsoluteCenter();
                e.Surface.SetCursorPosition(_mousePosition);
                e.Surface.RequestFocus(this);
                return true;
            default:
                return false;
        }
    }

    protected override bool OnCursorMove(CursorMoveSurfaceEvent e)
    {
        if (_captureMouse && !_ignoreNextMove)
        {
            var delta = e.Position - _mousePosition;
            if (!(float.Abs(delta.X) > 0) && !(float.Abs(delta.Y) > 0)) return true;

            OnMouseDelta(delta);

            _mousePosition = GetAbsoluteCenter();
            _ignoreNextMove = true;
            e.Surface.SetCursorPosition(_mousePosition);
            return true;
        }

        if (_ignoreNextMove)
        {
            _ignoreNextMove = false;
            _mousePosition = e.Position;
        }

        return base.OnCursorMove(e);
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2();
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        commands.Add(new DrawViewportCommand(_targetCamera, GetContentSize(), transform, _worldRenderer));
        commands.AddText("Noto Sans", GetModeText(), transform);
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