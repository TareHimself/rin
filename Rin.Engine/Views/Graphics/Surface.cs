using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Math;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics.Commands;
using Rin.Engine.Views.Graphics.Passes;
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
    private readonly SGraphicsModule _sGraphicsModule;
    private bool _isCursorIn;
    private CursorDownSurfaceEvent? _lastCursorDownEvent;
    public FrameStats Stats;

    protected Surface()
    {
        _sGraphicsModule = SEngine.Get().GetModule<SGraphicsModule>();
        _rootView.NotifyAddedToSurface(this);
    }

    public View? FocusedView { get; private set; }

    public virtual void Dispose()
    {
        _sGraphicsModule.WaitDeviceIdle();
        _rootView.Dispose();
    }

    public void Update(float deltaTime)
    {
        if (_isCursorIn) DoHover();
        _rootView.Update(deltaTime);
    }

    public abstract Vector2 GetCursorPosition();

    public abstract void SetCursorPosition(Vector2 position);

    public abstract void StartTyping(View view);
    public abstract void StopTyping(View view);
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
    // public virtual void EnsurePass(ViewsFrame frame, string passId, Action<ViewsFrame> applyPass)
    // {
    //     if (frame.ActivePass == passId) return;
    //     if (frame.ActivePass.Length > 0 && frame.ActivePass != passId) EndActivePass(frame);
    //     applyPass.Invoke(frame);
    //     frame.ActivePass = passId;
    // }

    // public virtual void BeginMainPass(ViewsFrame frame, bool clearColor = false, bool clearStencil = false)
    // {
    //     EnsurePass(frame, MainPassId, _ =>
    //     {
    //         var cmd = frame.Raw.GetCommandBuffer();
    //
    //         var size = frame.SurfaceSize;
    //
    //         var drawExtent = new VkExtent3D
    //         {
    //             width = (uint)size.X,
    //             height = (uint)size.Y
    //         };
    //
    //         cmd.BeginRendering(new VkExtent2D
    //             {
    //                 width = drawExtent.width,
    //                 height = drawExtent.height
    //             }, [
    //                 frame.DrawImage.MakeColorAttachmentInfo(
    //                     clearColor ? new Vector4(0.0f) : null)
    //             ],
    //             stencilAttachment: frame.StencilImage.MakeStencilAttachmentInfo(clearStencil ? 0 : null));
    //
    //         frame.Raw.ConfigureForViews(size);
    //
    //         if (clearStencil) ResetStencilState(cmd);
    //     });
    // }
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

    // public virtual void EndActivePass(ViewsFrame frame)
    // {
    //     frame.Raw.GetCommandBuffer().EndRendering();
    //     frame.ActivePass = "";
    // }

    private void ProcessPendingCommands(IEnumerable<ICommand> drawCommands,
        SharedPassContext context, List<IPass> passes)
    {
        List<ICommand> currentCommands = [];
        Type? currentPassType = null;

        foreach (var pendingCommand in drawCommands)
        {
            if (pendingCommand is NoOpCommand && currentCommands.NotEmpty())
            {
                passes.Add(currentCommands.First().CreatePass(new PassCreateInfo(context, currentCommands.ToArray())));
                continue;
            }

            if (currentCommands.Empty())
            {
                currentCommands.Add(pendingCommand);
                currentPassType = pendingCommand.PassType;
            }
            else
            {
                var passType = pendingCommand.PassType;
                if (currentPassType == passType)
                {
                    currentCommands.Add(pendingCommand);
                }
                else
                {
                    passes.Add(currentCommands.First()
                        .CreatePass(new PassCreateInfo(context, currentCommands.ToArray())));
                    currentCommands = [pendingCommand];
                    currentPassType = passType;
                }
            }
        }

        if (currentCommands.NotEmpty())
            passes.Add(currentCommands.First().CreatePass(new PassCreateInfo(context, currentCommands.ToArray())));
        Stats.BatchedDrawCommandCount++;
    }

    [PublicAPI]
    protected SharedPassContext? BuildPasses(IGraphBuilder builder)
    {
        var rawDrawCommands = new CommandList();
        _rootView.Collect(Matrix4x4.Identity, new Rect
        {
            Size = GetSize()
        }, rawDrawCommands);

        var rawCommands = rawDrawCommands.Commands.OrderBy(c => c, new RawCommandComparer()).ToArray();

        if (rawCommands.Length == 0) return null;

        Stats.InitialCommandCount = rawCommands.Length;

        var clips = rawDrawCommands.Clips;

        // var result = new PassInfo();

        var size = GetSize();
        var context = new SharedPassContext(new Extent2D
        {
            Width = (uint)float.Ceiling(size.X),
            Height = (uint)float.Ceiling(size.Y)
        });

        List<IPass> passes = [new CreateImagesPass(context)];
        {
            var uniqueClipStacks = rawDrawCommands.UniqueClipStacks;
            Dictionary<string, uint> computedClipStacks = [];
            List<ICommand> pendingCommands = [];
            uint currentMask = 0x01;
            foreach (var rawCommand in rawCommands)
            {
                if (currentMask == 128)
                {
                    ProcessPendingCommands(pendingCommands, context, passes);
                    pendingCommands.Clear();
                    computedClipStacks.Clear();
                    currentMask = 0x01;
                    passes.Add(new StencilClearPass(context));
                    Stats.StencilClearCount++;
                }

                if (rawCommand.ClipId.Length <= 0)
                {
                    rawCommand.Cmd.StencilMask = 0x01;
                    pendingCommands.Add(rawCommand.Cmd);
                }
                else if (computedClipStacks.TryGetValue(rawCommand.ClipId, out var stack))
                {
                    rawCommand.Cmd.StencilMask = stack;
                    pendingCommands.Add(rawCommand.Cmd);
                }
                else
                {
                    currentMask <<= 1;
                    passes.Add(new StencilWritePass(context, currentMask, uniqueClipStacks[rawCommand.ClipId].Select(
                        c => new StencilClip(clips[(int)c].Transform, clips[(int)c].Size)).ToArray()));
                    Stats.StencilWriteCount++;
                    //finalDrawCommands.AddRange(uniqueClipStacks[rawCommand.ClipId].Select(clipId => clips[(int)clipId]).Select(clip => new FinalDrawCommand() { Type = CommandType.ClipDraw, ClipInfo = clip, Mask = currentMask }));
                    computedClipStacks.Add(rawCommand.ClipId, currentMask);
                    rawCommand.Cmd.StencilMask = currentMask;
                    pendingCommands.Add(rawCommand.Cmd);
                }
            }

            if (pendingCommands.Count != 0)
            {
                ProcessPendingCommands(pendingCommands, context, passes);
                pendingCommands.Clear();
            }
        }

        foreach (var pass in passes) builder.AddPass(pass);
        return context;
        // return result;
    }

    protected virtual void ReceiveCursorEnter(CursorMoveSurfaceEvent e)
    {
        _isCursorIn = true;
    }

    protected virtual void ReceiveCursorLeave()
    {
        _isCursorIn = false;
        foreach (var view in _lastHovered) view.NotifyCursorLeave();

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
}