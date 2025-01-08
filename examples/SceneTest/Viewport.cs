
using JetBrains.Annotations;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.FrameGraph;
using rin.Framework.Graphics.Windows;
using rin.Framework.Scene;
using rin.Framework.Scene.Components;
using rin.Framework.Scene.Entities;
using rin.Framework.Views;
using rin.Framework.Views.Content;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Commands;
using SceneTest.entities;
using TerraFX.Interop.Vulkan;

namespace SceneTest;

public enum EViewportChannel
{
    Scene,
    Color,
    Location,
    Normal,
    RoughnessMetallicSpecular,
    Emissive
}

internal class CreateSceneBuffersPass(CameraEntity camera, Vec2<uint> size) : IPass
{
    /// <summary>
    /// Color Image Id
    /// </summary>
    public uint ColorImageId { get; private set; }
    /// <summary>
    /// Location Image Id
    /// </summary>
    public uint LocationImageId { get; private set; }
    /// <summary>
    /// Normal Image ID
    /// </summary>
    public uint NormalImageId { get; private set; }
    /// <summary>
    /// Roughness Metallic Specular Image ID
    /// </summary>
    public uint RMSImageId { get; private set; }
    
    /// <summary>
    /// Emissive Image ID
    /// </summary>
    public uint EmissiveImageId { get; private set; }
    
    /// <summary>
    /// Depth Image ID
    /// </summary>
    public uint DepthImageId { get; private set; }
    
    // public DeviceImage Color;
    // public DeviceImage Location;
    // public DeviceImage Normal;
    // public DeviceImage RoughnessMetallicSpecular;
    // public DeviceImage Emissive;
    
    [PublicAPI]
    public IDeviceImage? ColorImage { get; set; }
    
    [PublicAPI]
    public IDeviceImage? LocationImage { get; set; }
    
    [PublicAPI]
    public IDeviceImage? NormalImage { get; set; }
    
    [PublicAPI]
    public IDeviceImage? RMSImage { get; set; }
    
    [PublicAPI]
    public IDeviceImage? EmissiveImage { get; set; }
    
    [PublicAPI]
    public IDeviceImage? DepthImage { get; set; }
    
    public void Dispose()
    {

    }

    public void Configure(IGraphConfig config)
    {
        ColorImageId = config.CreateImage(size.X, size.Y,ImageFormat.Rgba32);
        LocationImageId = config.CreateImage(size.X, size.Y,ImageFormat.Rgba32);
        NormalImageId = config.CreateImage(size.X, size.Y,ImageFormat.Rgba32);
        RMSImageId = config.CreateImage(size.X, size.Y,ImageFormat.Rgba32);
        EmissiveImageId = config.CreateImage(size.X, size.Y,ImageFormat.Rgba32);
        DepthImageId = config.CreateImage(size.X, size.Y,ImageFormat.Depth);
    }

    public void Execute(ICompiledGraph graph, Frame frame, VkCommandBuffer cmd)
    {
        ColorImage = graph.GetResource(ColorImageId).AsImage();
        LocationImage = graph.GetResource(LocationImageId).AsImage();
        NormalImage = graph.GetResource(NormalImageId).AsImage();
        RMSImage = graph.GetResource(RMSImageId).AsImage();
        EmissiveImage = graph.GetResource(EmissiveImageId).AsImage();
        DepthImage = graph.GetResource(DepthImageId).AsImage();
        
        IDeviceImage[] colorImages = [ColorImage,LocationImage,NormalImage,RMSImage,EmissiveImage];
        var colorAttachments = colorImages.Select(c =>
        {
            cmd.ImageBarrier(c, ImageLayout.Undefined, ImageLayout.ColorAttachment);
            return c.MakeColorAttachmentInfo(0.0f);
        }).ToArray();
        var depthAttachment = DepthImage.MakeDepthAttachmentInfo();
        
        
        cmd.BeginRendering(size.ToVkExtent(),colorAttachments,depthAttachment);
        cmd
            .SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
            .DisableStencilTest(false)
            .DisableCulling()
            .EnableDepthTest(true,VkCompareOp.VK_COMPARE_OP_LESS_OR_EQUAL)
            .EnableBlendingAdditive(0, 5)
            .SetViewports([
                new VkViewport()
                {
                    x = 0,
                    y = 0,
                    width = size.X,
                    height = size.Y,
                    minDepth = 0.0f,
                    maxDepth = 1.0f
                }
            ])
            .SetScissors([
                new VkRect2D()
                {
                    offset = new VkOffset2D(),
                    extent = new VkExtent2D()
                    {
                        width = size.X,
                        height = size.Y
                    }
                }
            ]);
        
        var comp = camera.GetCameraComponent();
        var scene = comp.Owner?.Scene;
        if (scene != null)
        {
            var aspect = (float)size.X / (float)size.Y;
            var projection = Glm.Perspective(comp.FieldOfView, aspect, comp.NearClipPlane, comp.FarClipPlane);
            var view = (Mat4)comp.GetWorldTransform();
            var viewProj = view * projection;
            
            var roots = scene.GetPureRoots().ToArray();
            var drawCommands = new rin.Framework.Scene.Graphics.DrawCommands();
            foreach (var root in roots)
            {
                root.Collect(drawCommands,new Mat4());
            }
            var lights = drawCommands.Lights.ToArray();
        }
        cmd.EndRendering();
        
        foreach (var deviceImage in colorImages)
        {
            cmd.ImageBarrier(deviceImage, ImageLayout.ColorAttachment, ImageLayout.ShaderReadOnly);
        }
    }

    public uint Id { get; set; }

    public string Name => "Create Scene Buffers";
    public bool IsTerminal => false;
}
internal class DisplaySceneCommand(CreateSceneBuffersPass pass,Vec2<uint> size,EViewportChannel channel) : CustomCommand
{
    // public override void Run(WidgetFrame frame)
    // {
    //     var drawer = scene.Drawer;
    //     var source = channel switch
    //     {
    //         EViewportChannel.Scene => drawer?.RenderTarget,
    //         EViewportChannel.Color => drawer?.Images?.Color,
    //         EViewportChannel.Location => drawer?.Images?.Location,
    //         EViewportChannel.Normal => drawer?.Images?.Normal,
    //         EViewportChannel.RoughnessMetallicSpecular => drawer?.Images?.RoughnessMetallicSpecular,
    //         EViewportChannel.Emissive => drawer?.Images?.Emissive,
    //         _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
    //     };
    //     
    //     if(drawer == null || source == null) return;
    //     
    //     if(frame.IsMainPassActive) frame.Surface.EndActivePass(frame);
    //     
    //     drawer.Draw(frame);
    //     
    //     var cmd = frame.Raw.GetCommandBuffer();
    //     
    //     source.Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL);
    //     frame.Surface.GetDrawImage().Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL);
    //     source.CopyTo(cmd,frame.Surface.GetDrawImage());
    //     frame.Surface.GetDrawImage().Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);
    // }
    public override bool WillDraw => true;

    public override ulong MemoryNeeded => 0;

    public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBuffer? buffer = null)
    {
        // Do Final lighting and brdf stuff here, also maybe some post processing
    }
}

public class Viewport : ContentView
{
    private readonly CameraEntity _targetCamera;
    private EViewportChannel _channel = EViewportChannel.Scene;
    private readonly TextBox _modeText;
    private bool _ignoreNextMove = false;
    private Vec2<float> _mousePosition = 0.0f;
    private bool _captureMouse = false;

    public Vec2<float> AbsoluteCenter => (GetContentSize() / 2.0f).ApplyTransformation(ComputeAbsoluteTransform());
    
    public Viewport(CameraEntity camera,TextBox modeText)
    {
        _targetCamera = camera;
        _modeText = modeText;
        UpdateModeText();
    }

    public void UpdateModeText()
    {
        _modeText.Content = _channel switch
        {
            EViewportChannel.Scene => "Default",
            EViewportChannel.Color => "Color",
            EViewportChannel.Location => "Location",
            EViewportChannel.Normal => "Normal",
            EViewportChannel.RoughnessMetallicSpecular => "Roughness Metallic Specular",
            EViewportChannel.Emissive => "Emissive",
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
            _channel = (EViewportChannel)currentIdx;
            UpdateModeText();
            return true;
        }

        if (e.Button == CursorButton.Two)
        {
            _captureMouse = true;
            _ignoreNextMove = true;
            _mousePosition = AbsoluteCenter;
            e.Surface.SetCursorPosition(_mousePosition);
            return true;
        }

        return false;
    }

    protected override bool OnCursorMove(CursorMoveEvent e)
    {
        if (_captureMouse)
        {
            var delta = e.Position - _mousePosition;

            if (!(Math.Abs(delta.X) > 0) && !(Math.Abs(delta.Y) > 0)) return true;
            
            OnMouseDelta(delta);
            _mousePosition = AbsoluteCenter;
            e.Surface.SetCursorPosition(_mousePosition);
            
            return true;
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
        var scenePass = new CreateSceneBuffersPass(_targetCamera, size);
        drawCommands.AddBuilder((builder) =>
        {
            scenePassId = builder.AddPass(scenePass);
        });
        drawCommands.AddConfigure((_,config) =>
        {
            config.DependOn(scenePassId);
        });
        drawCommands.Add(new DisplaySceneCommand(scenePass, size, _channel));
    }

    protected void OnMouseDelta(Vec2<float> delta)
    {
        Console.WriteLine($"Mouse Moved X : {delta.X} , Y : {delta.Y} ");
        var viewTarget = _targetCamera?.RootComponent;
        if(viewTarget == null) return;
        viewTarget.SetRelativeRotation(viewTarget.GetRelativeRotation().ApplyYaw(delta.X).ApplyPitch(delta.Y));
    }
}