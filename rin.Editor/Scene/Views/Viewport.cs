using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Editor.Scene.Components;
using Rin.Editor.Scene.Graphics;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Commands;
using TerraFX.Interop.Vulkan;

namespace Rin.Editor.Scene.Views;

public enum ViewportChannel : int
{
    Scene,
    Color,
    Location,
    Normal,
    RoughnessMetallicSpecular,
    Emissive
}

internal class SetupRenderingCommand(CameraComponent camera,Vector2<uint> size) : UtilityCommand
{
    [PublicAPI]
    public ForwardRenderingPass RenderPass = new ForwardRenderingPass(camera,size);

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
        return;
    }
}
internal class DisplaySceneCommand(ForwardRenderingPass renderingPass,Vector2 size, Mat3 transform) : CustomCommand
{
    
    struct PushConstant
    {
        public Mat4 Projection;
        public Vector2 Size;
        public Mat3 Transform;
    }

    private readonly IShader _shader = SGraphicsModule.Get()
        .GraphicsShaderFromPath(Path.Join(SGraphicsModule.ShadersDirectory, "scene", "forward", "viewport.slang"));

    

    public override ulong GetRequiredMemory() => 0;
    public override bool WillDraw() => true;


    public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBufferView? view = null)
    {
        var cmd = frame.Raw.GetCommandBuffer();
        if (renderingPass.OutputImage is {} outputImage && _shader.Bind(cmd))
        {
            frame.BeginMainPass();
            var pushResource = _shader.PushConstants.Values.First();
            var descriptor = frame.Raw.GetDescriptorAllocator()
                     .Allocate(_shader.GetDescriptorSetLayouts().Values.First());
            descriptor.WriteImages(0, new ImageWrite(outputImage, ImageLayout.ShaderReadOnly,
                ImageType.Sampled, new SamplerSpec()
                {
                    Filter = ImageFilter.Linear,
                    Tiling = ImageTiling.ClampEdge
                }));
            
            cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,_shader.GetPipelineLayout(),[descriptor]);
            cmd.PushConstant(_shader.GetPipelineLayout(), pushResource.Stages, new PushConstant()
            {
                Projection = frame.Projection,
                Size = size,
                Transform = transform
            });
            
            cmd.Draw(6, 1);
        }
    }
}

public class Viewport : ContentView
{
    private readonly CameraComponent _targetCamera;
    private ViewportChannel _channel = ViewportChannel.Scene;
    private readonly TextBox _modeText;
    private bool _ignoreNextMove;
    private Vector2 _mousePosition;
    private bool _captureMouse;

    public override bool IsFocusable => true;
    
    protected Vector2 GetAbsoluteCenter() => (GetContentSize() / 2.0f).ApplyTransformation(ComputeAbsoluteTransform());

    public Viewport(CameraComponent camera, TextBox modeText)
    {
        _targetCamera = camera;
        _modeText = modeText;
        UpdateModeText();
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
            if(IsFocused) e.Surface.ClearFocus();
        }

        base.OnCursorUp(e);
    }

    public override bool OnCursorDown(CursorDownSurfaceEvent e)
    {
        if (e.Button == CursorButton.One)
        {
            var currentIdx = (int)_channel;
            currentIdx = (currentIdx + 1) % 5;
            _channel = (ViewportChannel)currentIdx;
            UpdateModeText();
            return true;
        }

        if (e.Button == CursorButton.Two)
        {
            _captureMouse = true;
            _ignoreNextMove = true;
            _mousePosition = GetAbsoluteCenter();
            e.Surface.SetCursorPosition(_mousePosition);
            e.Surface.RequestFocus(this);
            return true;
        }

        return false;
    }

    protected override void OnCursorMove(CursorMoveSurfaceEvent e)
    {
        if (_captureMouse && !_ignoreNextMove)
        {
            var delta = e.Position - _mousePosition;

            if (!(Math.Abs(delta.X) > 0) && !(Math.Abs(delta.Y) > 0)) return true;

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

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        var contentSize = GetContentSize();
        var size = new Vector2<uint>((uint)Math.Ceiling(contentSize.X), (uint)Math.Ceiling(contentSize.Y));
        var renderingCmd = new SetupRenderingCommand(_targetCamera,size);
        commands
            .Add(renderingCmd)
        .Add(new DisplaySceneCommand(renderingCmd.RenderPass,contentSize,transform));
    }
    
    

    protected virtual void OnMouseDelta(Vector2 delta)
    {
        // //Console.WriteLine($"Mouse Moved X : {delta.X} , Y : {delta.Y} ");
        // var viewTarget = _targetCamera?.RootComponent;
        // if (viewTarget == null) return;//.ApplyYaw(delta.X).ApplyPitch(delta.Y)
        // viewTarget.SetRelativeRotation(viewTarget.GetRelativeRotation().Delta(pitch: delta.Y, yaw: delta.X));
    }
}