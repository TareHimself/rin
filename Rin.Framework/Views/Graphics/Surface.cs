using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Extensions;
using Rin.Framework.Math;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.FrameGraph;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics.CommandHandlers;
using Rin.Framework.Views.Graphics.Commands;
using Rin.Framework.Views.Graphics.Passes;

namespace Rin.Framework.Views.Graphics;

/// <summary>
/// Base class for a surface that can display views
/// </summary>
public abstract class Surface : ISurface
{
    private readonly List<IView> _lastHovered = [];
    private readonly Root _rootView = new();
    private readonly SGraphicsModule _sGraphicsModule;
    private bool _isCursorIn;
    private CursorDownSurfaceEvent? _lastCursorDownEvent;

    protected Surface()
    {
        _sGraphicsModule = SApplication.Get().GetModule<SGraphicsModule>();
        _rootView.NotifyAddedToSurface(this);
    }

    public IView? FocusedView { get; private set; }

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

    public virtual bool RequestFocus(IView requester)
    {
        if (FocusedView == requester) return true;
        if (!requester.IsFocusable || !requester.IsHitTestable) return false;

        if (FocusedView is not null) ClearFocus();
        FocusedView = requester;
        requester.OnFocus();
        return true;
    }

    private void ProcessPendingCommands(IEnumerable<ICommand> drawCommands,
        SurfaceContext context, List<IPass> passes)
    {
        List<ICommand> currentCommands = [];
        List<ICommandHandler> currentHandlers = [];
        Type? currentPassConfigType = null;
        Type? currentHandlerType = null;

        foreach (var cmd in drawCommands)
        {
            // NoOpCommand's are like breaks so we break any batching that can happen here
            if (cmd is NoOpCommand && currentCommands.NotEmpty())
            {
                if (currentCommands.Count > 0)
                    currentHandlers.Add(currentCommands.First().CreateHandler(currentCommands.ToArray()));

                passes.Add(new ViewsDrawPass(context,currentCommands.First().CreateConfig(context), currentHandlers.ToArray()));
                
                currentCommands.Clear();
                currentHandlers.Clear();
                //passes.Add(new ViewsDrawPass(currentCommands.First().CreateConfig(context),currentHandlers.ToArray()));
                //passes.Add(currentCommands.First().CreatePass(new PassCreateInfo(context, currentCommands.ToArray())));
                continue;
            }

            if (currentCommands.Count > 0)
            {
                var firstCmd = currentCommands.First();
                if (currentHandlerType != cmd.HandlerType)
                {
                    currentHandlers.Add(firstCmd.CreateHandler(currentCommands.ToArray()));
                    currentCommands.Clear();
                }

                if (currentPassConfigType != cmd.PassConfigType)
                {
                    passes.Add(new ViewsDrawPass(context,firstCmd.CreateConfig(context),
                        currentHandlers.ToArray()));
                    currentHandlers.Clear();
                }
            }

            currentCommands.Add(cmd);
            currentHandlerType = cmd.HandlerType;
            currentPassConfigType = cmd.PassConfigType;
        }

        if (currentCommands.NotEmpty())
        {
            currentHandlers.Add(currentCommands.First().CreateHandler(currentCommands.ToArray()));
            passes.Add(new ViewsDrawPass(context,currentCommands.First().CreateConfig(context), currentHandlers.ToArray()));
        }
    }

    [PublicAPI]
    public SurfaceContext? BuildPasses(IGraphBuilder builder)
    {
        var size = GetSize();
        
        var drawList = new CommandList()
        {
            SurfaceSize = size
        };
        _rootView.Collect(Matrix4x4.Identity, new Rect2D
        {
            Size = size
        }, drawList);

        var rawCommands = drawList.Commands;

        if (rawCommands.Count == 0) return null;
        
        var clips = drawList.Clips;

        var context = new SurfaceContext(new Extent2D
        {
            Width = (uint)float.Ceiling(size.X),
            Height = (uint)float.Ceiling(size.Y)
        });

        List<IPass> passes = [new CreateImagesPass(context)];
        {
            var uniqueClipStacks = drawList.UniqueClipStacks;
            Dictionary<string, uint> computedClipMasks = [];
            List<ICommand> pendingCommands = [];
            uint shifted = 1;
            uint currentMask = 0x2;
            foreach (var (command, clipId) in drawList.Commands.Zip(drawList.ClipIds))
            {
                if (shifted == 31)
                {
                    ProcessPendingCommands(pendingCommands, context, passes);
                    pendingCommands.Clear();
                    computedClipMasks.Clear();
                    currentMask = 0x02;
                    shifted = 1;
                    passes.Add(new StencilClearPass(context));
                }


                if (clipId.Length <= 0) // No clipping
                {
                    command.StencilMask = 0x01;
                    pendingCommands.Add(command);
                }
                else if (computedClipMasks.TryGetValue(clipId, out var clipMask))
                {
                    command.StencilMask = clipMask;
                    pendingCommands.Add(command);
                }
                else
                {
                    passes.Add(new StencilWritePass(context, currentMask,
                        uniqueClipStacks[clipId]
                            .Select(c => new StencilClip(clips[(int)c].Transform, clips[(int)c].Size)).ToArray()));
                    computedClipMasks.Add(clipId, currentMask);
                    command.StencilMask = currentMask;
                    pendingCommands.Add(command);
                    currentMask <<= 1;
                    shifted++;
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

    public virtual void ReceiveCursorEnter(CursorMoveSurfaceEvent e)
    {
        _isCursorIn = true;
    }

    public virtual void ReceiveCursorLeave()
    {
        _isCursorIn = false;
        foreach (var view in _lastHovered) view.NotifyCursorLeave();

        _lastHovered.Clear();
    }

    public virtual void ReceiveResize(ResizeSurfaceEvent e)
    {
        _rootView.ComputeSize(e.Size);
    }

    public virtual void ReceiveCursorDown(CursorDownSurfaceEvent e)
    {
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

    public virtual void ReceiveCursorUp(CursorUpSurfaceEvent e)
    {
        OnCursorUp?.Invoke(e);
        if (_lastCursorDownEvent is { } lastEvent)
        {
            lastEvent.Target?.OnCursorUp(e);
            _lastCursorDownEvent = null;
        }
    }

    public virtual void ReceiveCursorMove(CursorMoveSurfaceEvent e)
    {
        _rootView.HandleEvent(e, Matrix4x4.Identity);
        _lastHovered.AddRange(e.Over);
        // Maybe leave this to the event handler in the future
        if (_lastCursorDownEvent is { } lastEvent)
        {
            if(_lastCursorDownEvent.Target == e.Target) return;
            
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

    public virtual void ReceiveScroll(ScrollSurfaceEvent e)
    {
        _rootView.HandleEvent(e, Matrix4x4.Identity);
    }

    public virtual void ReceiveCharacter(CharacterSurfaceEvent e)
    {
        FocusedView?.OnCharacter(e);
    }

    public virtual void ReceiveKeyboard(KeyboardSurfaceEvent e)
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