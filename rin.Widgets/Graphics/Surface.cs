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
    private Vector2<float>? _lastMousePosition;
    
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
        _rootWidget.ComputeSize(e.Size.Cast<float>());
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
                SGraphicsModule.MakeRenderingAttachment(frame.DrawImage.View,
                    VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL, clearColor
                        ? new VkClearValue
                        {
                            color = SGraphicsModule.MakeClearColorValue(1.0f)
                        }
                        : null)
            ], stencilAttachment: SGraphicsModule.MakeRenderingAttachment(frame.StencilImage.View,
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
        _rootWidget.Collect(Matrix3.Identity,new Rect()
        {
            Size = GetDrawSize().Cast<float>()
        }, rawDrawCommands);
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
    public virtual void Draw(Frame frame)
    {
        DoHover();
        if (Stats.InitialCommandCount != 0)
        {
            Stats = new FrameStats();
        }
        var drawCommands = CollectDrawCommands();
        
        if (drawCommands.Count == 0)
        {
            return;
        }

        frame.GetBuilder().AddPass(new WidgetPass(this, drawCommands.ToArray()));
    }

    public virtual void ReceiveCursorDown(CursorDownEvent e)
    {
        var point = e.Position.Cast<float>();
        if (_rootWidget.NotifyCursorDown(e, Matrix3.Identity) is { } result)
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
        _rootWidget.NotifyCursorMove(e, Matrix3.Identity);
    }

    public virtual void ReceiveScroll(ScrollEvent e)
    {
        _rootWidget.NotifyScroll(e, Matrix3.Identity);
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
            var rootTransformInfo = new Rect()
            {
                Size = 0.0f
            };
            if (0.0f <= e.Position && e.Position <= GetDrawSize().Cast<float>())
                _rootWidget.NotifyCursorEnter(e,Matrix3.Identity, _lastHovered);
        }

        var hoveredSet = _lastHovered.ToHashSet();


        foreach (var widget in oldHoverList.AsReversed())
            if (!hoveredSet.Contains(widget))
                widget.NotifyCursorLeave(e);
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