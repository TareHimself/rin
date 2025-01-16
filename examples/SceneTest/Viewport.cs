using System.Runtime.InteropServices;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Descriptors;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Graphics.Windows;
using rin.Framework.Scene.Graphics;
using rin.Framework.Views;
using rin.Framework.Views.Content;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Commands;
using SceneTest.entities;
using TerraFX.Interop.Vulkan;
using DrawCommands = rin.Framework.Views.Graphics.DrawCommands;

namespace SceneTest;

public enum ViewportChannel : int
{
    Scene,
    Color,
    Location,
    Normal,
    RoughnessMetallicSpecular,
    Emissive
}

internal class DisplaySceneCommand(CollectScenePass pass,ViewportChannel channel, Vec2<float> size, Mat3 transform) : CustomCommand
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct RenderData
    {
        public ViewportChannel Channel;
        public Vec2<float> Size;
        public Mat3 Transform;
        public Mat4 View;
        public Mat4 Projection;
        public int NumLights;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PushConstant
    {
        public Mat4 Projection;
        public ulong Quads;
        public ulong Lights;
    }

    private readonly IShader _shader = SGraphicsModule.Get()
        .GraphicsShaderFromPath(Path.Join(SGraphicsModule.ShadersDirectory, "scene", "mesh", "viewport.slang"));

    public override bool WillDraw => true;

    public override ulong MemoryNeeded => (ulong)(Marshal.SizeOf<RenderData>() + Marshal.SizeOf<LightInfo>() * pass.Lights.Length);

    public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBuffer? buffer = null)
    {
        var cmd = frame.Raw.GetCommandBuffer();
        if (buffer != null && _shader.Bind(cmd))
        {
            frame.BeginMainPass();
            var descriptor = frame.Raw.GetDescriptorAllocator()
                .Allocate(_shader.GetDescriptorSetLayouts().Values.First());
            var resource1 = _shader.Resources["RGB_COLOR_A_ROUGHNESS"];
            var resource2 = _shader.Resources["RGB_LOCATION_A_METALLIC"];
            var resource3 = _shader.Resources["RGB_NORMAL_A_SPECULAR"];
            var pushResource = _shader.PushConstants.Values.First();
            var sampler = new SamplerSpec()
            {
                Filter = ImageFilter.Linear,
                Tiling = ImageTiling.ClampEdge
            };
            descriptor.WriteImages(
                resource1.Binding,
                new ImageWrite(
                    pass.ColorRoughnessImage!,
                    ImageLayout.ShaderReadOnly, ImageType.Texture,
                    sampler)
            );
            descriptor.WriteImages(
                resource2.Binding,
                new ImageWrite(
                    pass.LocationMetallicImage!,
                    ImageLayout.ShaderReadOnly, ImageType.Texture,
                    sampler)
            );
            descriptor.WriteImages(
                resource3.Binding,
                new ImageWrite(
                    pass.NormalSpecularImage!,
                    ImageLayout.ShaderReadOnly, ImageType.Texture,
                    sampler)
            );
            
            buffer.Write(new RenderData()
            {
                Channel = channel,
                Size = size,
                Transform = transform,
                View = pass.View,
                Projection = pass.Projection,
                NumLights = pass.Lights.Length
            });

            using var lightsBuffer = buffer.GetView((ulong)Marshal.SizeOf<RenderData>(),(ulong)(Marshal.SizeOf<LightInfo>() * pass.Lights.Length));
            
            lightsBuffer.Write(pass.Lights);
            
            cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,_shader.GetPipelineLayout(),[descriptor]);
            cmd.PushConstant(_shader.GetPipelineLayout(), pushResource.Stages, new PushConstant()
            {
                Projection = frame.Projection,
                Quads = buffer.GetAddress(),
                Lights = lightsBuffer.GetAddress(),
            });

            cmd.Draw(6, 1);
        }
        // Do Final lighting and brdf stuff here, also maybe some post processing
    }
}

public class Viewport : ContentView
{
    private readonly CameraActor _targetCamera;
    private ViewportChannel _channel = ViewportChannel.Scene;
    private readonly TextBox _modeText;
    private bool _ignoreNextMove = false;
    private Vec2<float> _mousePosition = 0.0f;
    private bool _captureMouse = false;

    public Vec2<float> GetAbsoluteCenter() => (GetContentSize() / 2.0f).ApplyTransformation(ComputeAbsoluteTransform());

    public Viewport(CameraActor camera, TextBox modeText)
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

    protected override Vec2<float> LayoutContent(Vec2<float> availableSpace)
    {
        return availableSpace;
    }

    // public override void SetSize(Vector2<float> size)
    // {
    //     base.SetSize(size);
    //     TargetScene.Drawer?.Resize(new Vector2<uint>((uint)Math.Ceiling(size.Width),(uint)Math.Ceiling(size.Height)));
    // }

    public override void OnCursorUp(CursorUpEvent e)
    {
        if (_captureMouse)
        {
            _captureMouse = false;
            _ignoreNextMove = false;
        }

        base.OnCursorUp(e);
    }

    public override bool OnCursorDown(CursorDownEvent e)
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
            return true;
        }

        return false;
    }

    protected override bool OnCursorMove(CursorMoveEvent e)
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

    protected override Vec2<float> ComputeDesiredContentSize()
    {
        return 0.0f;
    }

    public override void CollectContent(Mat3 transform, DrawCommands drawCommands)
    {
        var contentSize = GetContentSize();
        var size = new Vec2<uint>((uint)Math.Ceiling(contentSize.X), (uint)Math.Ceiling(contentSize.Y));
        uint scenePassId = 0;
        var scenePass = new CollectScenePass(_targetCamera.GetCameraComponent(), size);
        drawCommands.AddBuilder((builder) => { scenePassId = builder.AddPass(scenePass); });
        drawCommands.AddConfigure((_, config) => { config.DependOn(scenePassId); });
        drawCommands.Add(new DisplaySceneCommand(scenePass, _channel,contentSize,transform));
    }

    protected void OnMouseDelta(Vec2<float> delta)
    {
        //Console.WriteLine($"Mouse Moved X : {delta.X} , Y : {delta.Y} ");
        var viewTarget = _targetCamera?.RootComponent;
        if (viewTarget == null) return;//.ApplyYaw(delta.X).ApplyPitch(delta.Y)
        viewTarget.SetRelativeRotation(viewTarget.GetRelativeRotation().Delta(pitch: delta.Y, yaw: delta.X));
    }
}