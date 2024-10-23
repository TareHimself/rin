using System.Runtime.InteropServices;
using aerox.Runtime.Extensions;
using aerox.Runtime.Graphics;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Containers;
using aerox.Runtime.Widgets.Events;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Widgets.Graphics;

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
public abstract class Surface : Disposable
{
    private readonly List<Widget> _lastHovered = [];
    private readonly WCRoot _rootWidget = new WCRoot();
    private readonly SGraphicsModule _sGraphicsModule;
    private DeviceImage? _copyImage;
    private DeviceImage? _drawImage;
    private DeviceImage? _stencilImage;
    private Vector2<float>? _lastMousePosition;
    public Widget? FocusedWidget { get; private set; }
    public event Action<CursorUpEvent>? OnCursorUp;


    public static readonly string MainPassId = Guid.NewGuid().ToString();
    public Surface()
    {
        _sGraphicsModule = SRuntime.Get().GetModule<SGraphicsModule>();
        _rootWidget.NotifyAddedToSurface(this);
    }
    
    public virtual void Init()
    {
        CreateImages();
    }

    public abstract Vector2<int> GetDrawSize();

    // private void UpdateProjectionMatrix()
    // {
    //     var size = GetDrawSize();
    //     _globalData.Projection = Glm.Orthographic(0, size.X, 0, size.Y);
    // }
    //
    // private void UpdateViewport()
    // {
    //     var size = GetDrawSize();
    //     _globalData.Viewport = new Vector4<float>(0, 0, size.X, size.Y);
    // }

    protected override void OnDispose(bool isManual)
    {
        _sGraphicsModule.WaitDeviceIdle();
        _rootWidget.Dispose();
        _drawImage?.Dispose();
        _copyImage?.Dispose();
        _stencilImage?.Dispose();
    }


    private void CreateImages()
    {
        var imageExtent = GetDrawSize().Cast<uint>();

        const VkImageUsageFlags imageUsageFlags = VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT |
                                                  VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT |
                                                  VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT |
                                                  VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
        var drawCreateInfo =
            SGraphicsModule.MakeImageCreateInfo(ImageFormat.Rgba32, new VkExtent3D
            {
                width = imageExtent.X,
                height = imageExtent.Y,
                depth = 1
            }, imageUsageFlags);
        
        var stencilCreateInfo = SGraphicsModule.MakeImageCreateInfo(ImageFormat.Stencil, new VkExtent3D
        {
            width = imageExtent.X,
            height = imageExtent.Y,
            depth = 1
        }, VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT |
           VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT |
           VkImageUsageFlags.VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT);

        _drawImage = _sGraphicsModule.GetAllocator().NewDeviceImage(drawCreateInfo, "Widgets Draw Image");
        _copyImage = _sGraphicsModule.GetAllocator().NewDeviceImage(drawCreateInfo, "Widgets Copy Image");
        _stencilImage = _sGraphicsModule.GetAllocator().NewDeviceImage(stencilCreateInfo, "Widgets Stencil Image");

        _drawImage.View = _sGraphicsModule.CreateImageView(
            SGraphicsModule.MakeImageViewCreateInfo(_drawImage, VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT));
        
        _copyImage.View = _sGraphicsModule.CreateImageView(
            SGraphicsModule.MakeImageViewCreateInfo(_copyImage, VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT));
        
        _stencilImage.View = _sGraphicsModule.CreateImageView(
            SGraphicsModule.MakeImageViewCreateInfo(_stencilImage, VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT));
    }

    protected virtual void ClearFocus()
    {
        FocusedWidget?.OnFocusLost();
        FocusedWidget = null;
    }
    
    public virtual bool RequestFocus(Widget requester)
    {
        if (FocusedWidget == requester) return true;
        if (!requester.IsHitTestable()) return false;
        
        ClearFocus();
        FocusedWidget = requester;
        requester.OnFocus();
        return true;

    }


    public virtual void ReceiveResize(ResizeEvent e)
    {
        _drawImage?.Dispose();
        _copyImage?.Dispose();
        _stencilImage?.Dispose();
        CreateImages();
        _rootWidget.SetSize(e.Size);
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

    public virtual void BeginMainPass(WidgetFrame frame,bool clearColor = false,bool clearStencil = false)
    {
        if(frame.IsMainPassActive) return;
        
        var cmd = frame.Raw.GetCommandBuffer();
        
        var size = GetDrawSize();
        
        var drawExtent = new VkExtent3D
        {
            width = (uint)size.X,
            height = (uint)size.Y
        };

        cmd.BeginRendering(new VkExtent2D()
        {
            width = drawExtent.width,
            height = drawExtent.height
        }, [
            SGraphicsModule.MakeRenderingAttachment(GetDrawImage().View,
                VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL, clearColor
                    ? new VkClearValue
                    {
                        color = SGraphicsModule.MakeClearColorValue(0.0f)
                    }
                    : null)
        ],stencilAttachment:SGraphicsModule.MakeRenderingAttachment(GetDrawImage().View,
            VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL, clearColor
                ? new VkClearValue
                {
                    color = SGraphicsModule.MakeClearColorValue(0.0f)
                }
                : null));
        
        frame.Raw.ConfigureForWidgets(size.Cast<uint>());

        frame.ActivePass = MainPassId;
    }


    public virtual void EndMainPass(WidgetFrame frame)
    {
        frame.Raw.GetCommandBuffer().EndRendering();
        frame.ActivePass = "";
    }

    public virtual void BlitMainImageToCopyImage(Frame frame)
    {
        
    }

    public void DrawCommandsToFinalCommands(IEnumerable<PendingCommand> drawCommands,ref List<FinalDrawCommand> finalDrawCommands)
    {
        
    }
    
    public List<FinalDrawCommand> CollectDrawCommands()
    {
        var rawDrawCommands = new DrawCommands();
        var transformInfo = new TransformInfo(Matrix3.Identity, GetDrawSize().Cast<float>(), 0);
        _rootWidget.Collect(transformInfo,rawDrawCommands);
        var rawCommands = rawDrawCommands.Commands;
        if (rawCommands.Count == 0) return [];

        var clips = rawDrawCommands.Clips;

        if (clips.Count == 0)
        {
            List<FinalDrawCommand> finalDrawCommands = [];
            DrawCommandsToFinalCommands(rawCommands.Select(c => new PendingCommand(c.DrawCommand,0x01)), ref finalDrawCommands);
            return finalDrawCommands;
        }
        else
        {
            List<FinalDrawCommand> finalDrawCommands = [];
            var uniqueClipStacks = rawDrawCommands.UniqueClipStacks;
            Dictionary<string, uint> computedClipStacks = [];
            List<PendingCommand> pendingCommands = [];
            uint currentMask = 0x01;
            foreach (var rawCommand in rawCommands)
            {
                if (currentMask == 128)
                {
                    DrawCommandsToFinalCommands(pendingCommands, ref finalDrawCommands);
                    pendingCommands.Clear();
                    computedClipStacks.Clear();
                    currentMask = 0x01;
                    finalDrawCommands.Add(new FinalDrawCommand()
                    {
                        Type = CommandType.ClipClear
                    });
                }

                if (computedClipStacks.TryGetValue(rawCommand.ClipId, out var stack))
                {
                    pendingCommands.Add(new PendingCommand(rawCommand.DrawCommand,stack));
                }
                else
                {
                    currentMask <<= 1;
                    finalDrawCommands.AddRange(uniqueClipStacks[rawCommand.ClipId].Select(clipId => clips[(int)clipId]).Select(clip => new FinalDrawCommand() { Type = CommandType.ClipDraw, ClipInfo = clip, Mask = currentMask }));
                    computedClipStacks.Add(rawCommand.ClipId,currentMask);
                    pendingCommands.Add(new PendingCommand(rawCommand.DrawCommand,currentMask));
                }
            }

            if (pendingCommands.Count != 0)
            {
                DrawCommandsToFinalCommands(pendingCommands,ref finalDrawCommands);
                pendingCommands.Clear();
            }

            return finalDrawCommands;
        }
    }

    public virtual void HandleBeforeDraw(Frame frame)
    {
        var cmd = frame.GetCommandBuffer();
        _drawImage?.Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);
        _copyImage?.Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);
        _stencilImage?.Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,VkImageLayout.VK_IMAGE_LAYOUT_STENCIL_ATTACHMENT_OPTIMAL);
    }
    
    public virtual void HandleAfterDraw(Frame frame)
    {
        var cmd = frame.GetCommandBuffer();
        _drawImage?.Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL);
    }
    
    public virtual void HandleSkippedDraw(Frame frame)
    {
        _drawImage?.Barrier(frame.GetCommandBuffer(),VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL);
    }
    public virtual void Draw(Frame frame)
    {
        if (_drawImage == null || _copyImage == null || _stencilImage == null) return;
        
        DoHover();

        var drawCommands = CollectDrawCommands();
        
        if(drawCommands.Count == 0)
        {
            HandleSkippedDraw(frame);
            return;
        }

        var properties = new VkPhysicalDeviceProperties();

        unsafe
        {
            vkGetPhysicalDeviceProperties(SGraphicsModule.Get().GetPhysicalDevice(),&properties);
        }

        ulong memoryNeeded = 0;
        ulong finalMemoryNeeded = 0;
        var minOffsetAlignment = properties.limits.minStorageBufferOffsetAlignment;
        List<ulong> batchOffset = [];
        var widgetFrame = new WidgetFrame(this, frame);
        
        foreach (var drawCommand in drawCommands)
        {
            if (drawCommand.Type == CommandType.BatchedDraw)
            {
                batchOffset.Add(memoryNeeded);
                foreach (var size in drawCommand.Batch!.GetMemoryNeeded())
                {
                    finalMemoryNeeded = size;
                    memoryNeeded += size;
                    var dist = memoryNeeded & minOffsetAlignment;
                    memoryNeeded += dist > 0 ? minOffsetAlignment - dist : 0;
                }
            }
        }
        
        var isWritingStencil = false;
        var isComparingStencil = false;

        var faceFlags = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;
        var batchedDrawIndex = 0;
        var cmd = frame.GetCommandBuffer();

        var buffer = memoryNeeded > 0 ? SGraphicsModule.Get().GetAllocator().NewStorageBuffer(memoryNeeded) : null;
        HandleBeforeDraw(frame);
        // // Image we will draw on
        // _drawImage.Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
        //     VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,new ImageBarrierOptions()
        //     {
        //         WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT,
        //         NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT
        //     });
        //
        // // Copy image
        // _copyImage.Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
        //     VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,new ImageBarrierOptions()
        //     {
        //         WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT,
        //         NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT
        //     });
        
        for (var i = 0; i < drawCommands.Count; i++)
        {
            var command = drawCommands[i];

            switch (command.Type)
            {
                case CommandType.None:
                    break;
                case CommandType.ClipDraw:
                {
                    BeginMainPass(widgetFrame);

                    if (!isWritingStencil)
                    {
                        vkCmdSetStencilOp(cmd,faceFlags,VkStencilOp.VK_STENCIL_OP_KEEP,VkStencilOp.VK_STENCIL_OP_REPLACE,VkStencilOp.VK_STENCIL_OP_KEEP,VkCompareOp.VK_COMPARE_OP_ALWAYS);
                        cmd.SetColorBlendEnable(0, 1, false);
                        isWritingStencil = true;
                        isComparingStencil = false;
                    }
                    
                    vkCmdSetStencilWriteMask(cmd,faceFlags,command.Mask);
                    SWidgetsModule.Get().WriteStencil(frame,command.ClipInfo!.Transform,command.ClipInfo!.Size);
                }
                    break;
                case CommandType.ClipClear:
                {
                    BeginMainPass(widgetFrame);

                    var clearAttachment = new VkClearAttachment()
                    {
                        aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT,
                        clearValue = new VkClearValue()
                        {
                            color = SGraphicsModule.MakeClearColorValue(0.0f),
                            depthStencil = new VkClearDepthStencilValue()
                            {
                                stencil = 0
                            }
                        }
                    };
                    unsafe
                    {
                        var extent = _stencilImage.Extent;
                        var clearRect = new VkClearRect()
                        {
                            baseArrayLayer = 0,
                            layerCount = 1,
                            rect = new VkRect2D()
                            {
                                offset = new VkOffset2D()
                                {
                                    x = 0,
                                    y = 0
                                },
                                extent = new VkExtent2D()
                                {
                                    width = extent.width,
                                    height = extent.height
                                }
                            }
                        };
                        
                        vkCmdClearAttachments(cmd,1,&clearAttachment,1,&clearRect);
                    }
                }
                    break;
                case CommandType.BatchedDraw:
                case CommandType.CustomDraw:
                {
                    BeginMainPass(widgetFrame);

                    if (!isComparingStencil)
                    {
                        vkCmdSetStencilOp(cmd,faceFlags,VkStencilOp.VK_STENCIL_OP_KEEP,VkStencilOp.VK_STENCIL_OP_KEEP,VkStencilOp.VK_STENCIL_OP_KEEP,VkCompareOp.VK_COMPARE_OP_NOT_EQUAL);
                        cmd.SetColorBlendEnable(0, 1, true);
                        isWritingStencil = false;
                        isComparingStencil = false;
                    }
                    else
                    {
                        vkCmdSetStencilCompareMask(cmd,faceFlags,command.Mask);
                    }

                    switch (command)
                    {
                        case { Type: CommandType.CustomDraw, Custom: not null }:
                            command.Custom.Run(widgetFrame,command.Mask);
                            break;
                        case { Type: CommandType.BatchedDraw, Batch: not null } when buffer != null:
                        {
                            var batch = command.Batch!;
                            batch.GetRenderer().Draw(widgetFrame,batch,buffer,batchOffset[batchedDrawIndex]);
                            batchedDrawIndex++;
                            break;
                        }
                    }
                }
                    break;
            }
        }
        
        if(widgetFrame.IsMainPassActive) EndMainPass(widgetFrame);
        
        HandleAfterDraw(frame);
    }

    public virtual void ReceiveCursorDown(CursorDownEvent e)
    {
        var point = e.Position.Cast<float>();
        if (_rootWidget.ReceiveCursorDown(e, new TransformInfo(this)) is { } result)
        {
            if(FocusedWidget == null) return;
            
            var shouldKeepFocus = false;
            while (result.Parent != null)
            {
                if (result != FocusedWidget) continue;
                shouldKeepFocus = true;
                break;
            }

            if (shouldKeepFocus) return;
        }

        ClearFocus();
    }

    public virtual void ReceiveCursorUp(CursorUpEvent e)
    {
        OnCursorUp?.Invoke(e);
    }

    public virtual void ReceiveCursorMove(CursorMoveEvent e)
    {
        _lastMousePosition = e.Position;
        var point = _lastMousePosition.Value;
        _rootWidget.ReceiveCursorMove(e, new TransformInfo(this));
    }

    public virtual void ReceiveScroll(ScrollEvent e)
    {
        var point = e.Position.Cast<float>();
        _rootWidget.ReceiveScroll(e, new TransformInfo(this));
    }


    public abstract Vector2<float> GetCursorPosition();

    public abstract void SetCursorPosition(Vector2<float> position);

    public void DoHover()
    {
        var mousePosition = GetCursorPosition();

        var delta = _lastMousePosition == null ? new Vector2<float>(0, 0) : mousePosition - _lastMousePosition.Value;

        _lastMousePosition = mousePosition.Cast<float>();

        var e = new CursorMoveEvent(this, mousePosition);

        var info = new TransformInfo(this);

        var size = GetDrawSize();

        var oldHoverList = _lastHovered.ToArray();
        _lastHovered.Clear();

        {
            var rootTransformInfo = new TransformInfo(this);
            if (rootTransformInfo.PointWithin(e.Position))
            {
                _rootWidget.ReceiveCursorEnter(e,rootTransformInfo,_lastHovered);
            }
        }

        var hoveredSet = _lastHovered.ToHashSet();

        Container? lastParent = null;

        var curTransform = info;
        
        foreach (var widget in oldHoverList.AsReversed())
        {
            curTransform = lastParent != null ? lastParent.ComputeChildTransform(widget, info) : curTransform;
            lastParent = widget is Container asContainer ? asContainer : null;
            if (!hoveredSet.Contains(widget))
            {
                widget.ReceiveCursorLeave(e,curTransform);
            }
        }
    }

    public virtual T Add<T>() where T : Widget, new()
    {
        return Add(Activator.CreateInstance<T>());
    }

    public virtual T Add<T>(T widget) where T : Widget
    {
        _rootWidget.AddChild(widget);
        return widget;
    }

    public virtual bool Remove(Widget widget)
    {
        return _rootWidget.RemoveChild(widget);
    }
}