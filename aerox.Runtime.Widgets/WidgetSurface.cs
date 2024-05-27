using System.Runtime.InteropServices;
using aerox.Runtime.Extensions;
using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Draw.Commands;
using aerox.Runtime.Widgets.Events;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Widgets;

[StructLayout(LayoutKind.Sequential)]
public struct GlobalWidgetShaderData
{
    public float time;
    public Vector4<float> Viewport;
    public Matrix4 Projection;
}

/// <summary>
///     Base class for a surface that can display widgets
/// </summary>
public abstract class WidgetSurface : Disposable
{
    private readonly List<Widget> _lastHovered = [];
    private readonly List<Widget> _rootWidgets = [];

    private readonly SGraphicsModule _sGraphicsModule;
    public readonly MaterialInstance SimpleRectMat;
    private DeviceImage? _copyImage;
    private DeviceImage? _drawImage;

    private GlobalWidgetShaderData _globalData = new()
    {
        Viewport = new Vector4<float>(0),
        time = 0.0f
    };

    private Vector2<float>? _lastMousePosition;


    public WidgetSurface()
    {
        _sGraphicsModule = SRuntime.Get().GetModule<SGraphicsModule>();
        _globalData.Viewport = new Vector4<float>(0, 0, 0, 0);
        GlobalBuffer = _sGraphicsModule.GetAllocator()
            .NewUniformBuffer<GlobalWidgetShaderData>(debugName: "Widget Root Global Buffer");
        GlobalBuffer.Write(_globalData);
        SimpleRectMat = SWidgetsModule.CreateMaterial(@$"{SRuntime.SHADERS_DIR}\2d\simple_rect.vert",
            @$"{SRuntime.SHADERS_DIR}\2d\simple_rect.frag");
        SimpleRectMat.BindBuffer("ui", GlobalBuffer);
    }

    public DeviceBuffer GlobalBuffer { get; }
    public event Action<CursorUpEvent> OnCursorUp;

    public virtual void Init()
    {
        CreateImages();
        UpdateProjectionMatrix();
        UpdateViewport();
        var size = GetDrawSize();
        _globalData.Viewport = new Vector4<float>(0, 0, size.X, size.Y);
        GlobalBuffer.Write(_globalData);
    }

    public abstract Vector2<int> GetDrawSize();

    private void UpdateProjectionMatrix()
    {
        var size = GetDrawSize();
        _globalData.Projection = Glm.Orthographic(0, size.X, 0, size.Y);
    }

    private void UpdateViewport()
    {
        var size = GetDrawSize();
        _globalData.Viewport = new Vector4<float>(0, 0, size.X, size.Y);
    }

    protected override void OnDispose(bool isManual)
    {
        _sGraphicsModule.WaitDeviceIdle();

        foreach (var widget in _rootWidgets) widget.Dispose();

        SimpleRectMat.Dispose();
        _rootWidgets.Clear();
        GlobalBuffer.Dispose();
        _drawImage?.Dispose();
        _copyImage?.Dispose();
    }


    private void CreateImages()
    {
        var imageExtent = GetDrawSize().Cast<uint>();

        const VkImageUsageFlags imageUsageFlags = VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT |
                                                  VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT |
                                                  VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT |
                                                  VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
        var drawCreateInfo =
            SGraphicsModule.MakeImageCreateInfo(VkFormat.VK_FORMAT_R16G16B16A16_SFLOAT, new VkExtent3D
            {
                width = imageExtent.X,
                height = imageExtent.Y,
                depth = 1
            }, imageUsageFlags);

        _drawImage = _sGraphicsModule.GetAllocator().NewDeviceImage(drawCreateInfo, "Widgets Draw Image");
        _copyImage = _sGraphicsModule.GetAllocator().NewDeviceImage(drawCreateInfo, "Widgets Temp Image");

        _drawImage.View = _sGraphicsModule.CreateImageView(
            SGraphicsModule.MakeImageViewCreateInfo(_drawImage, VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT));
        _copyImage.View = _sGraphicsModule.CreateImageView(
            SGraphicsModule.MakeImageViewCreateInfo(_copyImage, VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT));
    }


    public virtual void ReceiveResize(ResizeEvent e)
    {
        UpdateProjectionMatrix();
        UpdateViewport();
        _drawImage?.Dispose();
        _copyImage?.Dispose();
        CreateImages();
        foreach (var widget in _rootWidgets) widget.SetDrawSize(e.Size);
    }

    public DeviceImage GetDrawImage()
    {
        if (_drawImage == null) throw new Exception("Cannot Access Device Image Before it Has Been Created");
        return _drawImage;
    }

    public DeviceImage GetCopyImage()
    {
        if (_copyImage == null) throw new Exception("Cannot Access Device Image Before it Has Been Created");
        return _copyImage;
    }

    public virtual void Draw(Frame frame)
    {
        if (_drawImage == null || _copyImage == null) return;

        if (_rootWidgets.Count == 0)
        {
            SGraphicsModule.ImageBarrier(frame.GetCommandBuffer(), _drawImage,
                VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL);
            return;
        }

        var size = GetDrawSize();
        _globalData.time = (float)SRuntime.Get().GetTimeSinceCreation();

        GlobalBuffer.Write(_globalData);

        var drawInfo = DrawInfo.From(this);
        var widgetFrame = new WidgetFrame(this, frame);

        DoHover();

        // Collect Draw Commands
        foreach (var widget in _rootWidgets)
        {
            var widgetDrawInfo = drawInfo.AccountFor(widget);
            widget.Draw(widgetFrame, widgetDrawInfo);
        }


        // Do Actual Draw
        var cmd = frame.GetCommandBuffer();

        // Image we will draw on
        SGraphicsModule.ImageBarrier(cmd, _drawImage, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);

        // Copy image
        SGraphicsModule.ImageBarrier(cmd, _copyImage, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);

        var drawExtent = new VkExtent3D
        {
            width = (uint)size.X,
            height = (uint)size.Y
        };


        var renderingInfo = SGraphicsModule.MakeRenderingInfo(new VkExtent2D
        {
            width = drawExtent.width,
            height = drawExtent.height
        });

        unsafe
        {
            var colorAttachment =
                SGraphicsModule.MakeRenderingAttachment(GetDrawImage().View,
                    VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL, new VkClearValue
                    {
                        color = SGraphicsModule.MakeClearColorValue(0.0f)
                    });

            renderingInfo.colorAttachmentCount = 1;
            renderingInfo.pColorAttachments = &colorAttachment;

            vkCmdBeginRendering(cmd, &renderingInfo);
        }


        foreach (var widgetCmd in widgetFrame.DrawCommands)
            // Change pass during a readBack command
            if (widgetCmd is ReadBack readBackCommand)
            {
                vkCmdEndRendering(cmd);


                SGraphicsModule.ImageBarrier(cmd, _drawImage, VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
                    VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, new ImageBarrierOptions
                    {
                        SrcStageFlags = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT,
                        DstStageFlags = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT
                    });
                SGraphicsModule.ImageBarrier(cmd, _copyImage, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
                    VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, new ImageBarrierOptions
                    {
                        SrcStageFlags = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT,
                        DstStageFlags = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT
                    });

                SGraphicsModule.CopyImageToImage(cmd, _drawImage, _copyImage);

                SGraphicsModule.ImageBarrier(cmd, _drawImage, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                    VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL, new ImageBarrierOptions
                    {
                        SrcStageFlags = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT,
                        DstStageFlags = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT
                    });
                SGraphicsModule.ImageBarrier(cmd, _copyImage, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                    VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
                    new ImageBarrierOptions
                    {
                        SrcStageFlags = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT,
                        DstStageFlags = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT
                    });


                unsafe
                {
                    var colorAttachment =
                        SGraphicsModule.MakeRenderingAttachment(_drawImage.View,
                            VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);

                    renderingInfo.colorAttachmentCount = 1;
                    renderingInfo.pColorAttachments = &colorAttachment;

                    vkCmdBeginRendering(cmd, &renderingInfo);
                }

                readBackCommand.Bind(widgetFrame);
                readBackCommand.SetImageInput(_copyImage);
                readBackCommand.Run(widgetFrame);
            }
            else
            {
                widgetCmd.Bind(widgetFrame);
                widgetCmd.Run(widgetFrame);
            }

        vkCmdEndRendering(cmd);


        SGraphicsModule.ImageBarrier(cmd, GetDrawImage(), VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
            VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL);
    }

    public virtual void ReceiveCursorDown(CursorDownEvent e)
    {
        var point = e.Position.Cast<float>();
        ;
        var info = DrawInfo.From(this);
        foreach (var widget in _rootWidgets.AsReversed())
        {
            var widgetDrawInfo = info.AccountFor(widget);

            if (!widgetDrawInfo.PointWithin(point)) continue;

            if (!widget.IsHitTestable()) continue;

            if (!widget.ReceiveCursorDown(e, widgetDrawInfo)) continue;
            break;
        }
    }

    public virtual void ReceiveCursorUp(CursorUpEvent e)
    {
        OnCursorUp?.Invoke(e);
    }

    public virtual void ReceiveCursorMove(CursorMoveEvent e)
    {
        _lastMousePosition = e.Position;
        var point = _lastMousePosition.Value;
        var info = DrawInfo.From(this);

        foreach (var widget in _rootWidgets.AsReversed())
        {
            var widgetDrawInfo = info.AccountFor(widget);

            if (!widgetDrawInfo.PointWithin(point)) continue;

            if (!widget.IsHitTestable()) continue;

            if (!widget.ReceiveCursorMove(e, widgetDrawInfo)) continue;

            break;
        }
    }

    public virtual void ReceiveScroll(ScrollEvent e)
    {
        var point = e.Position.Cast<float>();
        var info = DrawInfo.From(this);

        foreach (var widget in _rootWidgets.AsReversed())
        {
            var widgetDrawInfo = info.AccountFor(widget);

            if (!widgetDrawInfo.PointWithin(point)) continue;

            if (!widget.IsHitTestable()) continue;

            if (!widget.ReceiveScroll(e, widgetDrawInfo)) continue;

            break;
        }
    }


    public abstract Vector2<float> GetCursorPosition();

    public void DoHover()
    {
        var mousePosition = GetCursorPosition();

        var delta = _lastMousePosition == null ? new Vector2<float>(0, 0) : mousePosition - _lastMousePosition.Value;

        _lastMousePosition = mousePosition.Cast<float>();
        ;

        var e = new CursorMoveEvent(this, mousePosition);

        var info = DrawInfo.From(this);

        var size = GetDrawSize();

        if (mousePosition.X < 0 || mousePosition.Y < 0 || mousePosition.X > size.X || mousePosition.Y > size.Y)
        {
            foreach (var widget in _lastHovered) widget.ReceiveCursorLeave(e, info);
            _lastHovered.Clear();
            return;
        }


        var previousLastHovered = _lastHovered.ToArray();
        _lastHovered.Clear();

        foreach (var widget in _rootWidgets.AsReversed())
        {
            var widgetDrawInfo = info.AccountFor(widget);

            if (!widgetDrawInfo.PointWithin(e.Position)) continue;

            if (!widget.IsHitTestable()) continue;

            widget.ReceiveCursorEnter(e, widgetDrawInfo, _lastHovered);
            break;
        }

        var hoveredSet = _lastHovered.ToHashSet();

        foreach (var widget in previousLastHovered)
            if (!hoveredSet.Contains(widget))
            {
                var widgetDrawInfo = info.AccountFor(widget);
                widget.ReceiveCursorLeave(e, widgetDrawInfo);
            }
    }

    public virtual T Add<T>() where T : Widget, new()
    {
        return Add(Activator.CreateInstance<T>());
    }

    public virtual T Add<T>(T widget) where T : Widget
    {
        widget.SetDrawSize(GetDrawSize());
        widget.SetRelativeOffset(new Vector2<float>(0, 0));
        widget.NotifyAddedToRoot(this);
        _rootWidgets.Add(widget);
        return widget;
    }

    public virtual bool Remove(Widget widget)
    {
        if (!_rootWidgets.Remove(widget)) return false;
        widget.NotifyRemovedFromRoot(this);
        return true;
    }
}