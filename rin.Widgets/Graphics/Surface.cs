using System.Runtime.InteropServices;
using rin.Core;
using rin.Core.Extensions;
using rin.Core.Math;
using rin.Graphics;
using rin.Widgets.Containers;
using rin.Widgets.Events;
using rin.Widgets.Graphics.Commands;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Widgets.Graphics;

/// <summary>
///     Base class for a surface that can display widgets
/// </summary>
public abstract class Surface : Disposable
{
    public static readonly string MainPassId = Guid.NewGuid().ToString();
    private readonly List<Widget> _lastHovered = [];
    private readonly Root _rootWidget = new();
    private readonly SGraphicsModule _sGraphicsModule;
    private DeviceImage? _copyImage;
    private DeviceImage? _drawImage;
    private Vector2<float>? _lastMousePosition;
    private DeviceImage? _stencilImage;
    
    public FrameStats Stats { get; private set; } = new();

    public Surface()
    {
        _sGraphicsModule = SRuntime.Get().GetModule<SGraphicsModule>();
        _rootWidget.NotifyAddedToSurface(this);
    }

    public Widget? FocusedWidget { get; private set; }
    public event Action<CursorUpEvent>? OnCursorUp;

    public virtual void Init()
    {
        CreateImages();
        _rootWidget.Offset = (0.0f);
        _rootWidget.ComputeSize(GetDrawSize().Cast<float>());
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
        var imageSize = new VkExtent3D
        {
            width = imageExtent.X,
            height = imageExtent.Y,
            depth = 1
        };
        
        _drawImage = _sGraphicsModule.CreateImage(imageSize,ImageFormat.Rgba32,imageUsageFlags,debugName: "Widgets Draw Image");
        _copyImage = _sGraphicsModule.CreateImage(imageSize,ImageFormat.Rgba32,imageUsageFlags,debugName: "Widgets Copy Image");
        _stencilImage = _sGraphicsModule.CreateImage(imageSize,ImageFormat.Stencil,VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT |
            VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT |
            VkImageUsageFlags.VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT,debugName: "Widgets Stencil Image");
    }

    protected virtual void ClearFocus()
    {
        FocusedWidget?.OnFocusLost();
        FocusedWidget = null;
    }

    public virtual bool RequestFocus(Widget requester)
    {
        if (FocusedWidget == requester) return true;
        if (!requester.IsFocusable || !requester.IsHitTestable) return false;

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
        _rootWidget.ComputeSize(e.Size.Cast<float>());
    }

    public DeviceImage GetDrawImage()
    {
        if (_drawImage == null) throw new Exception("Cannot Access Device Image Before it Has Been Created");
        return _drawImage;
    }

    public DeviceImage GetStencilImage()
    {
        if (_stencilImage == null) throw new Exception("Cannot Access Device Image Before it Has Been Created");
        return _stencilImage;
    }

    public DeviceImage GetCopyImage()
    {
        if (_copyImage == null) throw new Exception("Cannot Access Device Image Before it Has Been Created");
        return _copyImage;
    }

    /// <summary>
    /// Returns true if the pass was not the active pass
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="passId"></param>
    /// <param name="applyPass"></param>
    /// <returns></returns>
    public virtual void EnsurePass(WidgetFrame frame, string passId,Action<WidgetFrame> applyPass)
    {
        if (frame.ActivePass == passId) return;
        if(frame.ActivePass.Length > 0 && frame.ActivePass != passId) EndActivePass(frame);
        applyPass.Invoke(frame);
        frame.ActivePass = passId;
    }

    public virtual void BeginMainPass(WidgetFrame frame, bool clearColor = false, bool clearStencil = false)
    {
        EnsurePass(frame,MainPassId, (_) =>
        {
            var cmd = frame.Raw.GetCommandBuffer();

            var size = GetDrawSize();

            var drawExtent = new VkExtent3D
            {
                width = (uint)size.X,
                height = (uint)size.Y
            };

            cmd.BeginRendering(new VkExtent2D
            {
                width = drawExtent.width,
                height = drawExtent.height
            }, [
                SGraphicsModule.MakeRenderingAttachment(GetDrawImage().View,
                    VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL, clearColor
                        ? new VkClearValue
                        {
                            color = SGraphicsModule.MakeClearColorValue(1.0f)
                        }
                        : null)
            ], stencilAttachment: SGraphicsModule.MakeRenderingAttachment(GetStencilImage().View,
                VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL, clearColor
                    ? new VkClearValue
                    {
                        color = SGraphicsModule.MakeClearColorValue(0.0f)
                    }
                    : null));

            frame.Raw.ConfigureForWidgets(size.Cast<uint>());

            if (clearStencil) ResetStencilState(cmd);
        });
    }

    private static void ResetStencilState(VkCommandBuffer cmd,
        VkStencilFaceFlags faceMask = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK)
    {
        vkCmdSetStencilTestEnable(cmd, 1);
        vkCmdSetStencilReference(cmd, faceMask, 255);
        vkCmdSetStencilWriteMask(cmd, faceMask, 0x01);
        vkCmdSetStencilCompareMask(cmd, faceMask, 0x01);
        vkCmdSetStencilOp(cmd, faceMask, VkStencilOp.VK_STENCIL_OP_KEEP, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkStencilOp.VK_STENCIL_OP_KEEP, VkCompareOp.VK_COMPARE_OP_NEVER);
    }

    public virtual void EndActivePass(WidgetFrame frame)
    {
        frame.Raw.GetCommandBuffer().EndRendering();
        frame.ActivePass = "";
    }

    public void DrawCommandsToFinalCommands(IEnumerable<PendingCommand> drawCommands,
        ref List<FinalDrawCommand> finalDrawCommands)
    {
        IBatch? activeBatch = null;
        uint currentClipMask = 0x01;
        foreach (var pendingCommand in drawCommands)
        {
            if (currentClipMask != pendingCommand.ClipId)
            {
                if (activeBatch != null)
                {
                    finalDrawCommands.Add(new FinalDrawCommand
                    {
                        Batch = activeBatch,
                        Mask = currentClipMask,
                        Type = CommandType.BatchedDraw
                    });
                    activeBatch = null;
                    Stats.BatchedDrawCommandCount++;
                }

                currentClipMask = pendingCommand.ClipId;
            }

            switch (pendingCommand.DrawCommand)
            {
                case BatchedCommand asBatchedCommand:
                {
                    if (activeBatch == null)
                    {
                        activeBatch = asBatchedCommand.GetBatchRenderer().NewBatch();
                    }
                    else
                    {
                        if (activeBatch.GetRenderer() != asBatchedCommand.GetBatchRenderer())
                        {
                            finalDrawCommands.Add(new FinalDrawCommand
                            {
                                Batch = activeBatch,
                                Mask = currentClipMask,
                                Type = CommandType.BatchedDraw
                            });
                            activeBatch = asBatchedCommand.GetBatchRenderer().NewBatch();
                            Stats.BatchedDrawCommandCount++;
                        }
                    }


                    activeBatch.AddFromCommand(asBatchedCommand);
                    break;
                }
                case CustomCommand asCustomCommand:
                {
                    if (activeBatch != null)
                    {
                        finalDrawCommands.Add(new FinalDrawCommand
                        {
                            Batch = activeBatch,
                            Mask = currentClipMask,
                            Type = CommandType.BatchedDraw
                        });
                        activeBatch = null;
                        Stats.BatchedDrawCommandCount++;
                    }

                    if (finalDrawCommands.LastOrDefault() is
                            { Type: CommandType.Custom, Custom: not null } asPreviousCustomCommand &&
                        asPreviousCustomCommand.Mask == currentClipMask)
                        if (asPreviousCustomCommand.Custom.CombineWith(asCustomCommand))
                            continue;

                    finalDrawCommands.Add(new FinalDrawCommand
                    {
                        Custom = asCustomCommand,
                        Mask = currentClipMask,
                        Type = CommandType.Custom
                    });
                    if (asCustomCommand.WillDraw)
                    {
                        Stats.NonBatchedDrawCommandCount++;
                    }
                    else
                    {
                        Stats.CustomCommandCount++;
                    }
                }
                    break;
            }
        }

        if (activeBatch == null) return;

        finalDrawCommands.Add(new FinalDrawCommand
        {
            Batch = activeBatch,
            Mask = currentClipMask,
            Type = CommandType.BatchedDraw
        });
        Stats.BatchedDrawCommandCount++;
    }

    public List<FinalDrawCommand> CollectDrawCommands()
    {
        var rawDrawCommands = new DrawCommands();
        var transformInfo = new TransformInfo(this);
        _rootWidget.Collect(transformInfo, rawDrawCommands);
        var rawCommands = rawDrawCommands.Commands.OrderBy(c => c, new RawCommandComparer()).ToArray();

        if (rawCommands.Length == 0) return [];

        Stats.InitialCommandCount = rawCommands.Length;
        
        var clips = rawDrawCommands.Clips;

        if (clips.Count == 0)
        {
            List<FinalDrawCommand> finalDrawCommands = [];
            DrawCommandsToFinalCommands(rawCommands.Select(c => new PendingCommand(c.Command, 0x01)),
                ref finalDrawCommands);
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
                    finalDrawCommands.Add(new FinalDrawCommand
                    {
                        Type = CommandType.ClipClear
                    });
                    Stats.StencilClearCount++;
                }

                if (rawCommand.ClipId.Length <= 0)
                {
                    pendingCommands.Add(new PendingCommand(rawCommand.Command, 0x01));
                }
                else if (computedClipStacks.TryGetValue(rawCommand.ClipId, out var stack))
                {
                    pendingCommands.Add(new PendingCommand(rawCommand.Command, stack));
                }
                else
                {
                    currentMask <<= 1;
                    finalDrawCommands.Add(new FinalDrawCommand
                    {
                        Type = CommandType.ClipDraw,
                        Clips = uniqueClipStacks[rawCommand.ClipId].Select(c => new StencilClip
                        {
                            Transform = clips[(int)c].Transform,
                            Size = clips[(int)c].Size
                        }).ToArray(),
                        Mask = currentMask
                    });
                    Stats.StencilWriteCount++;
                    //finalDrawCommands.AddRange(uniqueClipStacks[rawCommand.ClipId].Select(clipId => clips[(int)clipId]).Select(clip => new FinalDrawCommand() { Type = CommandType.ClipDraw, ClipInfo = clip, Mask = currentMask }));
                    computedClipStacks.Add(rawCommand.ClipId, currentMask);
                    pendingCommands.Add(new PendingCommand(rawCommand.Command, currentMask));
                    
                }
            }

            if (pendingCommands.Count != 0)
            {
                DrawCommandsToFinalCommands(pendingCommands, ref finalDrawCommands);
                pendingCommands.Clear();
            }

            return finalDrawCommands;
        }
    }

    public virtual void HandleBeforeDraw(Frame frame)
    {
        var cmd = frame.GetCommandBuffer();
        GetDrawImage().Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL);
        GetCopyImage().Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL);
        unsafe
        {
            fixed (VkClearColorValue* pColor = new[]
                       { SGraphicsModule.MakeClearColorValue(new Vector4<float>(0.0f, 0.0f, 0.0f, 1.0f)) })
            {
                fixed (VkImageSubresourceRange* pRanges = new[]
                           { SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT) })
                {
                    vkCmdClearColorImage(cmd, GetDrawImage().Image, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL, pColor, 1,
                        pRanges);
                    vkCmdClearColorImage(cmd, GetCopyImage().Image, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL, pColor, 1,
                        pRanges);
                }
            }

            GetDrawImage().Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL,
                VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);
            GetCopyImage().Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL,
                VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
            ResetStencilState(cmd);


            GetStencilImage().Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                VkImageLayout.VK_IMAGE_LAYOUT_GENERAL, new ImageBarrierOptions
                {
                    SubresourceRange = SGraphicsModule.MakeImageSubresourceRange(
                        VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT | VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT)
                });

            fixed (VkClearDepthStencilValue* pColor = new[]
                       { SGraphicsModule.MakeClearDepthStencilValue(stencil: 0) })
            {
                fixed (VkImageSubresourceRange* pRanges = new[]
                       {
                           SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT |
                                                                     VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT)
                       })
                {
                    vkCmdClearDepthStencilImage(cmd, GetStencilImage().Image, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL,
                        pColor, 1, pRanges);
                    ResetStencilState(cmd);
                }
            }

            GetStencilImage().Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL,
                VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL, new ImageBarrierOptions
                {
                    SubresourceRange = SGraphicsModule.MakeImageSubresourceRange(
                        VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT | VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT)
                });
        }
    }

    public virtual void HandleAfterDraw(Frame frame)
    {
        var cmd = frame.GetCommandBuffer();
        _drawImage?.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
            VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL);
    }

    public virtual void HandleSkippedDraw(Frame frame)
    {
        _drawImage?.Barrier(frame.GetCommandBuffer(), VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL);
    }

    public virtual void Draw(Frame frame)
    {
        if (_drawImage == null || _copyImage == null || _stencilImage == null) return;

        DoHover();
        if (Stats.InitialCommandCount != 0)
        {
            Stats = new FrameStats();
        }
        var drawCommands = CollectDrawCommands();
        
        if (drawCommands.Count == 0)
        {
            
            HandleSkippedDraw(frame);
            return;
        }
        
        // BeginMainPass(new WidgetFrame(this,frame),clearColor: true);
        // EndActivePass(new WidgetFrame(this,frame));
        // HandleAfterDraw(frame);
        // return;
        var properties = new VkPhysicalDeviceProperties();

        unsafe
        {
            vkGetPhysicalDeviceProperties(SGraphicsModule.Get().GetPhysicalDevice(), &properties);
        }

        ulong memoryNeeded = 0;
        List<Pair<ulong,ulong>> bufferOffsetsAndSizes = [];
        var widgetFrame = new WidgetFrame(this, frame);

        foreach (var drawCommand in drawCommands)
            if (drawCommand.Type == CommandType.BatchedDraw)
            {
                var offset = memoryNeeded;
                
                foreach (var size in drawCommand.Batch!.GetMemoryNeeded())
                {
                    // memoryNeeded += size;
                    // var dist = memoryNeeded & minOffsetAlignment;
                    // memoryNeeded += dist > 0 ? minOffsetAlignment - dist : 0;
                    memoryNeeded += size;
                }
                bufferOffsetsAndSizes.Add(new Pair<ulong, ulong>(offset,memoryNeeded - offset));
            }
            else if (drawCommand.Type == CommandType.ClipDraw)
            {
                var offset = memoryNeeded;
                memoryNeeded += (ulong)(Marshal.SizeOf<StencilClip>() * drawCommand.Clips.Length);
                bufferOffsetsAndSizes.Add(new Pair<ulong, ulong>(offset,memoryNeeded - offset));
                // var dist = memoryNeeded & minOffsetAlignment;
                // memoryNeeded += dist > 0 ? minOffsetAlignment - dist : 0;
            }
            else if (drawCommand is { Type: CommandType.Custom, Custom.MemoryNeeded: {} asCustomMemory } && asCustomMemory != 0)
            {
                var offset = (memoryNeeded);
                memoryNeeded += asCustomMemory;
                bufferOffsetsAndSizes.Add(new Pair<ulong, ulong>(offset,memoryNeeded - offset));
            }

        var isWritingStencil = false;
        var isComparingStencil = false;

        var faceFlags = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;
        var cmd = frame.GetCommandBuffer();

        var buffer = memoryNeeded > 0 ? SGraphicsModule.Get().GetAllocator().NewStorageBuffer(memoryNeeded) : null;
        
        Stats.FinalCommandCount = drawCommands.Count;
        Stats.MemoryAllocatedBytes = memoryNeeded;
        
        if (buffer != null) frame.OnReset += _ => buffer.Dispose();
        var offsetsAndSizes = bufferOffsetsAndSizes.GetEnumerator();
        offsetsAndSizes.MoveNext();
        HandleBeforeDraw(frame);
        //BeginMainPass(widgetFrame,true,true);
        // unsafe
        // {
        //     fixed (VkClearDepthStencilValue* pColor = new []
        //                { SGraphicsModule.MakeClearDepthStencilValue(stencil: 0) })
        //     {
        //         fixed (VkImageSubresourceRange* pRanges = new[]
        //                    { SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT) })
        //         {
        //             vkCmdClearDepthStencilImage(cmd,GetDrawImage().Image,VkImageLayout.VK_IMAGE_LAYOUT_GENERAL,pColor,1,pRanges); 
        //             ResetStencilState(cmd);
        //         }
        //     }
        // }

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
                        vkCmdSetStencilOp(cmd, faceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
                            VkStencilOp.VK_STENCIL_OP_REPLACE, VkStencilOp.VK_STENCIL_OP_KEEP,
                            VkCompareOp.VK_COMPARE_OP_ALWAYS);
                        cmd.SetWriteMask(0, 1, 0);
                        if (SWidgetsModule.Get().GetStencilShader() is { } stencilShader &&
                            stencilShader.Bind(cmd, true))
                        {
                            isWritingStencil = true;
                            isComparingStencil = false;
                        }
                    }

                    {
                        if (isWritingStencil && buffer != null && SWidgetsModule.Get().GetStencilShader() is
                                { } stencilShader)
                        {
                            vkCmdSetStencilWriteMask(cmd, faceFlags, command.Mask);
                            
                            var (offset,size) = offsetsAndSizes.Current;
                            offsetsAndSizes.MoveNext();
                            var view = buffer.GetView(offset, size);
                            frame.OnReset += (_) => view.Dispose();
                            
                            view.Write(command.Clips, offset);

                            var push = new StencilPushConstant
                            {
                                Projection = widgetFrame.Projection,
                                data = view.GetAddress()
                            };
                            var pushResource = stencilShader.PushConstants.First().Value;
                            cmd.PushConstant(stencilShader.GetPipelineLayout(), pushResource.Stages, push);

                            vkCmdDraw(cmd, 6, (uint)command.Clips.Length, 0, 0);
                        }
                    }
                }
                    break;
                case CommandType.ClipClear:
                {
                    if (widgetFrame.ActivePass.Length == 0) BeginMainPass(widgetFrame);

                    var clearAttachment = new VkClearAttachment
                    {
                        aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT,
                        clearValue = new VkClearValue
                        {
                            color = SGraphicsModule.MakeClearColorValue(0.0f),
                            depthStencil = new VkClearDepthStencilValue
                            {
                                stencil = 0
                            }
                        }
                    };
                    unsafe
                    {
                        var extent = _stencilImage.Extent;
                        var clearRect = new VkClearRect
                        {
                            baseArrayLayer = 0,
                            layerCount = 1,
                            rect = new VkRect2D
                            {
                                offset = new VkOffset2D
                                {
                                    x = 0,
                                    y = 0
                                },
                                extent = new VkExtent2D
                                {
                                    width = extent.width,
                                    height = extent.height
                                }
                            }
                        };

                        vkCmdClearAttachments(cmd, 1, &clearAttachment, 1, &clearRect);
                    }
                }
                    break;
                case CommandType.BatchedDraw:
                case CommandType.Custom:
                {
                    //BeginMainPass(widgetFrame);

                    if (!isComparingStencil)
                    {
                        vkCmdSetStencilOp(cmd, faceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
                            VkStencilOp.VK_STENCIL_OP_KEEP, VkStencilOp.VK_STENCIL_OP_KEEP,
                            VkCompareOp.VK_COMPARE_OP_NOT_EQUAL);
                        cmd.SetWriteMask(0, 1,
                            VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
                            VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
                            VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
                            VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT);
                        isWritingStencil = false;
                        isComparingStencil = true;
                    }

                    vkCmdSetStencilCompareMask(cmd, faceFlags, command.Mask);

                    switch (command)
                    {
                        case { Type: CommandType.Custom, Custom: not null }:
                        {
                            var willUseMemory = command.Custom.MemoryNeeded > 0;
                            if (willUseMemory && buffer != null)
                            {
                                var (offset,size) = offsetsAndSizes.Current;
                                offsetsAndSizes.MoveNext();
                                var view = buffer.GetView(offset, size);
                                frame.OnReset += (_) => view.Dispose();
                                command.Custom.Run(widgetFrame,command.Mask,view);
                            }
                            else
                            {
                                command.Custom.Run(widgetFrame, command.Mask);
                            }
                        }
                            break;
                        case { Type: CommandType.BatchedDraw, Batch: not null } when buffer != null:
                        {
                            var batch = command.Batch!;
                            var (offset, size) = offsetsAndSizes.Current;
                            offsetsAndSizes.MoveNext();
                            var view = buffer.GetView(offset, size);
                            frame.OnReset += (_) => view.Dispose();
                            batch.GetRenderer().Draw(widgetFrame, batch,view);
                            break;
                        }
                    }
                }
                    break;
            }
        }
        
        if (widgetFrame.ActivePass.Length > 0) EndActivePass(widgetFrame);
        
        buffer?.Dispose();
        HandleAfterDraw(frame);
    }

    public virtual void ReceiveCursorDown(CursorDownEvent e)
    {
        var point = e.Position.Cast<float>();
        if (_rootWidget.ReceiveCursorDown(e, new TransformInfo(this)) is { } result)
        {
            if (FocusedWidget == null) return;

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
    
    public virtual void ReceiveCharacter(CharacterEvent e)
    {
        FocusedWidget?.OnCharacter(e);
    }
    
    public virtual void ReceiveKeyboard(KeyboardEvent e)
    {
        FocusedWidget?.OnKeyboard(e);
    }
    
    public abstract Vector2<float> GetCursorPosition();

    public abstract void SetCursorPosition(Vector2<float> position);

    public void DoHover()
    {
        var mousePosition = GetCursorPosition();
        
        _lastMousePosition = mousePosition.Cast<float>();

        var e = new CursorMoveEvent(this, mousePosition);
        
        var oldHoverList = _lastHovered.ToArray();
        _lastHovered.Clear();

        {
            var rootTransformInfo = new TransformInfo(this);
            if (rootTransformInfo.PointWithin(e.Position))
                _rootWidget.ReceiveCursorEnter(e, rootTransformInfo, _lastHovered);
        }

        var hoveredSet = _lastHovered.ToHashSet();


        foreach (var widget in oldHoverList.AsReversed())
            if (!hoveredSet.Contains(widget))
                widget.ReceiveCursorLeave(e);
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