using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Math;
using Rin.Engine.Views;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Commands;
using Rin.Engine.Views.Graphics.Quads;
using Rin.Engine.World.Components;
using Rin.Engine.World.Graphics;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
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

internal class ViewPortPass(SharedPassContext info, DrawViewportCommand command) : IViewsPass
{
    private readonly ForwardRenderingPass _forwardPass = new(command.Camera, command.Size.ToExtent());
    private readonly Extent2D _renderExtent = command.Size.ToExtent();

    private readonly IShader _shader = SGraphicsModule.Get()
        .MakeGraphics("World/Shaders/viewport.slang");

    private uint _sceneImageId;
    private uint _viewportBufferId;
    public uint Id { get; set; }
    public bool IsTerminal => false;
    public bool HandlesPreAdd => true;
    public bool HandlesPostAdd => false;

    public void PreAdd(IGraphBuilder builder)
    {
        builder.AddPass(_forwardPass);
    }

    public void PostAdd(IGraphBuilder builder)
    {
    }

    public void Configure(IGraphConfig config)
    {
        _sceneImageId = config.ReadImage(_forwardPass.OutputImageId, ImageLayout.ShaderReadOnly);
        config.WriteImage(info.MainImageId, ImageLayout.ColorAttachment);
        config.ReadImage(info.StencilImageId, ImageLayout.StencilAttachment);
        _viewportBufferId = config.CreateBuffer<SceneData>(BufferStage.Graphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var cmd = ctx.GetCommandBuffer();
        if (_shader.Bind(cmd))
        {
            var sceneImage = graph.GetImageOrException(_sceneImageId);
            var mainImage = graph.GetImageOrException(info.MainImageId);
            var stencilImage = graph.GetImageOrException(info.StencilImageId);
            var buffer = graph.GetBufferOrException(_viewportBufferId);

            cmd.BeginRendering(_renderExtent.ToVk(), [
                    mainImage.MakeColorAttachmentInfo()
                ],
                stencilAttachment: stencilImage.MakeStencilAttachmentInfo()
            );
            cmd.SetViewState(_renderExtent);
            var faceFlags = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;

            vkCmdSetStencilOp(cmd, faceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
                VkStencilOp.VK_STENCIL_OP_KEEP, VkStencilOp.VK_STENCIL_OP_KEEP,
                VkCompareOp.VK_COMPARE_OP_NOT_EQUAL);

            cmd.SetWriteMask(0, 1,
                VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
                VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
                VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
                VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT);

            var pushResource = _shader.PushConstants.Values.First();
            var descriptor = ctx.DescriptorAllocator
                .Allocate(_shader.GetDescriptorSetLayouts().Values.First());
            descriptor.WriteImages(0, new ImageWrite(sceneImage, ImageLayout.ShaderReadOnly,
                DescriptorImageType.Sampled, new SamplerSpec
                {
                    Filter = ImageFilter.Linear,
                    Tiling = ImageTiling.ClampEdge
                }));

            buffer.Write(
                new SceneData
                {
                    Projection = info.ProjectionMatrix,
                    Transform = command.Transform,
                    Size = command.Size
                });
            cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, _shader.GetPipelineLayout(),
                [descriptor]);
            cmd.PushConstant(_shader.GetPipelineLayout(), pushResource.Stages, buffer.GetAddress());

            cmd.Draw(6);

            cmd.EndRendering();
        }
    }

    public static IViewsPass Create(PassCreateInfo info)
    {
        var cmd = info.Commands.First() as DrawViewportCommand ?? throw new NullReferenceException();
        return new ViewPortPass(info.Context, cmd);
    }

    private struct SceneData
    {
        public Matrix4x4 Projection;
        public Matrix4x4 Transform;
        public Vector2 Size;
    }
}

internal class DrawViewportCommand(CameraComponent camera, in Vector2 extent, in Matrix4x4 transform)
    : TCommand<ViewPortPass>
{
    public Vector2 Size { get; } = extent;
    public CameraComponent Camera { get; } = camera;
    public Matrix4x4 Transform { get; } = transform;
}

public class Viewport : ContentView
{
    private readonly CameraComponent _targetCamera;
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
        commands.Add(new DrawViewportCommand(_targetCamera, GetContentSize(), transform));
        commands.AddText("Noto Sans", GetModeText(), transform);
    }


    protected virtual void OnMouseDelta(Vector2 delta)
    {
        // //Console.WriteLine($"Mouse Moved X : {delta.X} , Y : {delta.Y} ");
        // var viewTarget = _targetCamera?.RootComponent;
        // if (viewTarget == null) return;//.ApplyYaw(delta.X).ApplyPitch(delta.Y)
        // viewTarget.SetRelativeRotation(viewTarget.GetRelativeRotation().Delta(pitch: delta.Y, yaw: delta.X));
    }
}