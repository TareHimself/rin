using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Core;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics.Commands;
using Rin.Engine.Core.Extensions;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Views.Graphics;

/// <summary>
///     Base class for a surface that can display views
/// </summary>
public abstract class Surface : IDisposable, IUpdatable
{
    public static readonly string MainPassId = Guid.NewGuid().ToString();
    private readonly List<View> _lastHovered = [];
    private readonly Root _rootView = new();
    private bool _isCursorIn;
    private readonly SGraphicsModule _sGraphicsModule;
    private CursorDownSurfaceEvent? _lastCursorDownEvent;
    public FrameStats Stats;

    protected Surface()
    {
        _sGraphicsModule = SEngine.Get().GetModule<SGraphicsModule>();
        _rootView.NotifyAddedToSurface(this);
    }
    
    public abstract Vector2 GetCursorPosition();

    public abstract void SetCursorPosition(Vector2 position);
    
    public View? FocusedView { get; private set; }
    public event Action<CursorUpSurfaceEvent>? OnCursorUp;

    public virtual void Init()
    {
        _rootView.Offset = default;
        _rootView.ComputeSize(GetSize());
    }

    public abstract Vector2 GetSize();

    
    public virtual void ClearFocus()
    {
        FocusedView?.OnFocusLost();
        FocusedView = null;
    }

    public virtual bool RequestFocus(View requester)
    {
        if (FocusedView == requester) return true;
        if (!requester.IsFocusable || !requester.IsHitTestable) return false;

        if (FocusedView is not null) ClearFocus();
        FocusedView = requester;
        requester.OnFocus();
        return true;
    }
    
    /// <summary>
    ///     Returns true if the pass was not the active pass
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="passId"></param>
    /// <param name="applyPass"></param>
    /// <returns></returns>
    public virtual void EnsurePass(ViewsFrame frame, string passId, Action<ViewsFrame> applyPass)
    {
        if (frame.ActivePass == passId) return;
        if (frame.ActivePass.Length > 0 && frame.ActivePass != passId) EndActivePass(frame);
        applyPass.Invoke(frame);
        frame.ActivePass = passId;
    }

    public virtual void BeginMainPass(ViewsFrame frame, bool clearColor = false, bool clearStencil = false)
    {
        EnsurePass(frame, MainPassId, _ =>
        {
            var cmd = frame.Raw.GetCommandBuffer();

            var size = frame.SurfaceSize;

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
                    frame.DrawImage.MakeColorAttachmentInfo(
                        clearColor ? new Vector4(0.0f) : null)
                ],
                stencilAttachment: frame.StencilImage.MakeStencilAttachmentInfo(clearStencil ? 0 : null));

            frame.Raw.ConfigureForViews(size);

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

    public virtual void EndActivePass(ViewsFrame frame)
    {
        frame.Raw.GetCommandBuffer().EndRendering();
        frame.ActivePass = "";
    }

    private void ProcessPendingCommands(IEnumerable<PendingCommand> drawCommands,
        ref PassInfo result)
    {
        IBatch? activeBatch = null;
        uint currentClipMask = 0x01;

        foreach (var pendingCommand in drawCommands)
        {
            if (pendingCommand.DrawCommand is UtilityCommand utilCmd)
            {
                if (utilCmd.Stage == CommandStage.Before)
                    result.PreCommands.Add(utilCmd);
                else
                    result.PostCommands.Add(utilCmd);
            }

            if (currentClipMask != pendingCommand.ClipId)
            {
                if (activeBatch != null)
                {
                    result.Commands.Add(new FinalDrawCommand
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
                            result.Commands.Add(new FinalDrawCommand
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
                        result.Commands.Add(new FinalDrawCommand
                        {
                            Batch = activeBatch,
                            Mask = currentClipMask,
                            Type = CommandType.BatchedDraw
                        });
                        activeBatch = null;
                        Stats.BatchedDrawCommandCount++;
                    }

                    if (result.Commands.LastOrDefault() is
                            { Type: CommandType.Custom, Custom: not null } asPreviousCustomCommand &&
                        asPreviousCustomCommand.Mask == currentClipMask)
                        if (asPreviousCustomCommand.Custom.CombineWith(asCustomCommand))
                            continue;

                    result.Commands.Add(new FinalDrawCommand
                    {
                        Custom = asCustomCommand,
                        Mask = currentClipMask,
                        Type = CommandType.Custom
                    });
                    if (asCustomCommand.WillDraw())
                        Stats.NonBatchedDrawCommandCount++;
                    else
                        Stats.CustomCommandCount++;
                }
                    break;
            }
        }

        if (activeBatch == null) return;

        result.Commands.Add(new FinalDrawCommand
        {
            Batch = activeBatch,
            Mask = currentClipMask,
            Type = CommandType.BatchedDraw
        });
        Stats.BatchedDrawCommandCount++;
    }

    [PublicAPI]
    protected PassInfo? ComputePassInfo()
    {
        var rawDrawCommands = new PassCommands();
        _rootView.Collect(Matrix4x4.Identity, new Rect
        {
            Size = GetSize()
        }, rawDrawCommands);

        var rawCommands = rawDrawCommands.Commands.OrderBy(c => c, new RawCommandComparer()).ToArray();

        if (rawCommands.Length == 0) return null;

        Stats.InitialCommandCount = rawCommands.Length;

        var clips = rawDrawCommands.Clips;

        var result = new PassInfo();

        if (clips.Count == 0)
        {
            List<FinalDrawCommand> finalDrawCommands = [];
            ProcessPendingCommands(rawCommands.Select(c => new PendingCommand(c.Command, 0x01)),
                ref result);
        }
        else
        {
            var uniqueClipStacks = rawDrawCommands.UniqueClipStacks;
            Dictionary<string, uint> computedClipStacks = [];
            List<PendingCommand> pendingCommands = [];
            uint currentMask = 0x01;
            foreach (var rawCommand in rawCommands)
            {
                {
                    if (rawCommand.Command is UtilityCommand utilCmd)
                    {
                        if (utilCmd.Stage == CommandStage.Before)
                            result.PreCommands.Add(utilCmd);
                        else
                            result.PostCommands.Add(utilCmd);
                    }
                }
                if (currentMask == 128)
                {
                    ProcessPendingCommands(pendingCommands, ref result);
                    pendingCommands.Clear();
                    computedClipStacks.Clear();
                    currentMask = 0x01;
                    result.Commands.Add(new FinalDrawCommand
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
                    result.Commands.Add(new FinalDrawCommand
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
                ProcessPendingCommands(pendingCommands, ref result);
                pendingCommands.Clear();
            }
        }

        return result;
    }

    protected virtual void ReceiveCursorEnter(CursorMoveSurfaceEvent e)
    {
        _isCursorIn = true;
    }

    protected virtual void ReceiveCursorLeave()
    {
        _isCursorIn = false;
        foreach (var view in _lastHovered)
        {
            view.NotifyCursorLeave();
        }
        
        _lastHovered.Clear();
    }

    protected virtual void ReceiveResize(ResizeSurfaceEvent e)
    {
        var size = e.Size.ToNumericsVector();
        _rootView.ComputeSize(size);
    }

    protected virtual void ReceiveCursorDown(CursorDownSurfaceEvent e)
    {
        var point = e.Position;
        _rootView.HandleEvent(e, Matrix4x4.Identity);
        if (e.Target is not null)
        {
            _lastCursorDownEvent = e;
            if (e.Target is { IsFocusable: true } target && FocusedView != target) RequestFocus(target);
        }
        else
        {
            _lastCursorDownEvent = null;
        }
    }

    protected virtual void ReceiveCursorUp(CursorUpSurfaceEvent e)
    {
        OnCursorUp?.Invoke(e);
        if (_lastCursorDownEvent is { } lastEvent)
        {
            lastEvent.Target?.OnCursorUp(e);
            _lastCursorDownEvent = null;
        }
    }

    protected virtual void ReceiveCursorMove(CursorMoveSurfaceEvent e)
    {
        _rootView.HandleEvent(e, Matrix4x4.Identity);
        _lastHovered.AddRange(e.Over);
        // Maybe leave this to the event handler in the future
        if (_lastCursorDownEvent is { } lastEvent)
        {
            var dist = lastEvent.Position.DistanceTo(e.Position);
            if (dist > 5.0)
            {
                var newEvent = new CursorDownSurfaceEvent(this, lastEvent.Button, e.Position);
                var parent = lastEvent.Target?.Parent;
                while (parent != null)
                {
                    newEvent.Target = parent;
                    if (parent.OnCursorDown(newEvent))
                    {
                        lastEvent.Target = parent;
                        lastEvent.Position = newEvent.Position;
                        break;
                    }

                    parent = parent.Parent;
                }

                _lastCursorDownEvent.Position = e.Position;
            }
        }
    }

    protected virtual void ReceiveScroll(ScrollSurfaceEvent e)
    {
        _rootView.HandleEvent(e, Matrix4x4.Identity);
    }

    protected virtual void ReceiveCharacter(CharacterSurfaceEvent e)
    {
        FocusedView?.OnCharacter(e);
    }

    protected virtual void ReceiveKeyboard(KeyboardSurfaceEvent e)
    {
        FocusedView?.OnKeyboard(e);
    }


    private void DoHover()
    {
        var mousePosition = GetCursorPosition();
        var e = new CursorMoveSurfaceEvent(this, mousePosition);
        var oldHoverList = _lastHovered.ToArray();
        _lastHovered.Clear();

        {
            if (e.Position.Within(new Vector2(), GetSize()))
            {
                _rootView.HandleEvent(e, Matrix4x4.Identity);
                _lastHovered.AddRange(e.Over);
            }
        }

        var hoveredSet = _lastHovered.ToHashSet();


        foreach (var view in oldHoverList.AsReversed())
            if (!hoveredSet.Contains(view))
                view.NotifyCursorLeave();
    }

    public virtual T Add<T>() where T : View, new()
    {
        return Add(Activator.CreateInstance<T>());
    }

    public virtual T Add<T>(T view) where T : View
    {
        _rootView.Add(view);
        return view;
    }

    public virtual bool Remove(View view)
    {
        return _rootView.Remove(view);
    }
    
    public virtual void Dispose()
    {
        _sGraphicsModule.WaitDeviceIdle();
        _rootView.Dispose();
    }

    public void Update(float deltaTime)
    {
        if (_isCursorIn)
        {
            DoHover();
        }
        _rootView.Update(deltaTime);
    }
}