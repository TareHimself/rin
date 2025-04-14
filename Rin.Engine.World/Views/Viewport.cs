using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Math;
using Rin.Engine.Views;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Commands;
using Rin.Engine.World.Components;
using Rin.Engine.World.Graphics;
using TerraFX.Interop.Vulkan;

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

internal class SetupRenderingCommand(CameraComponent camera, Vector2<uint> size) : UtilityCommand
{
    [PublicAPI] public ForwardRenderingPass RenderPass = new(camera, size);

    public override void BeforeAdd(IGraphBuilder builder)
    {
        builder.AddPass(RenderPass);
    }

    public override void Configure(IGraphConfig config)
    {
        config.DependOn(RenderPass.Id);
    }

    public override void Execute(ViewsFrame frame)
    {
    }
}

internal class DisplaySceneCommand(ForwardRenderingPass renderingPass, Vector2 size, Matrix4x4 transform)
    : CustomCommand
{
    private readonly IShader _shader = SGraphicsModule.Get()
        .MakeGraphics("World/Shaders/viewport.slang");

    public override ulong GetRequiredMemory()
    {
        return Utils.ByteSizeOf<Data>();
    }

    public override bool WillDraw()
    {
        return true;
    }


    public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBufferView? view = null)
    {
        var buffer = view ?? throw new NullReferenceException(nameof(view));
        var cmd = frame.Raw.GetCommandBuffer();
        if (renderingPass.OutputImage is { } outputImage && _shader.Bind(cmd))
        {
            frame.BeginMainPass();
            var pushResource = _shader.PushConstants.Values.First();
            var descriptor = frame.Raw.GetDescriptorAllocator()
                .Allocate(_shader.GetDescriptorSetLayouts().Values.First());
            descriptor.WriteImages(0, new ImageWrite(outputImage, ImageLayout.ShaderReadOnly,
                ImageType.Sampled, new SamplerSpec
                {
                    Filter = ImageFilter.Linear,
                    Tiling = ImageTiling.ClampEdge
                }));

            buffer.Write(
                new Data
                {
                    Projection = frame.Projection,
                    Transform = transform,
                    Size = size
                });
            cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, _shader.GetPipelineLayout(),
                [descriptor]);
            cmd.PushConstant(_shader.GetPipelineLayout(), pushResource.Stages, buffer.GetAddress());

            cmd.Draw(6);
        }
    }

    private struct Data
    {
        public Matrix4x4 Projection;
        public Matrix4x4 Transform;
        public Vector2 Size;
    }
}

public class Viewport : ContentView
{
    private readonly TextBox _modeText;
    private readonly CameraComponent _targetCamera;
    private bool _captureMouse;
    private ViewportChannel _channel = ViewportChannel.Scene;
    private bool _ignoreNextMove;
    private Vector2 _mousePosition;

    public Viewport(CameraComponent camera, TextBox modeText)
    {
        _targetCamera = camera;
        _modeText = modeText;
        UpdateModeText();
    }

    public override bool IsFocusable => true;

    protected Vector2 GetAbsoluteCenter()
    {
        return (GetContentSize() / 2.0f).Transform(ComputeAbsoluteTransform());
    }

    private void UpdateModeText()
    {
        _modeText.Content = _channel switch
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

    protected override Vector2 LayoutContent(Vector2 availableSpace)
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
                UpdateModeText();
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

    public override void CollectContent(Matrix4x4 transform, PassCommands commands)
    {
        var contentSize = GetContentSize();
        var size = new Vector2<uint>((uint)float.Ceiling(contentSize.X), (uint)float.Ceiling(contentSize.Y));
        var renderingCmd = new SetupRenderingCommand(_targetCamera, size);
        commands
            .Add(renderingCmd)
            .Add(new DisplaySceneCommand(renderingCmd.RenderPass, contentSize, transform));
    }


    protected virtual void OnMouseDelta(Vector2 delta)
    {
        // //Console.WriteLine($"Mouse Moved X : {delta.X} , Y : {delta.Y} ");
        // var viewTarget = _targetCamera?.RootComponent;
        // if (viewTarget == null) return;//.ApplyYaw(delta.X).ApplyPitch(delta.Y)
        // viewTarget.SetRelativeRotation(viewTarget.GetRelativeRotation().Delta(pitch: delta.Y, yaw: delta.X));
    }
}