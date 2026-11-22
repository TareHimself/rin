using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Events;
using Rin.Core.Extensions;
using Rin.Core.Shared.Math;
using Rin.Core.Shared.Pooling;

namespace Rin.Core.Views.Graphics;

/// <summary>
///     Base class for a surface that can display views
/// </summary>
public abstract class Surface : ISurface
{
    private readonly List<IView> _lastHovered = [];

    private readonly RootView _rootView = new()
    {
        Offset = default
    };

    private readonly IGraphicsModule _sGraphicsModule;
    private readonly HashSet<IView> _viewsPendingLayout = [];
    private bool _isCalculatingLayout;
    private bool _isCursorIn;
    private CursorDownSurfaceEvent? _lastCursorDownEvent;

    protected Surface(IGraphicsModule? graphicsModule = null)
    {
        _sGraphicsModule = graphicsModule ?? IGraphicsModule.Get();
        _rootView.NotifyAddedToSurface(this);
        _rootView.InvalidateLayout();
    }

    public IView? FocusedView { get; private set; }
    public virtual IClipboard Clipboard => _sGraphicsModule.Clipboard;

    public virtual void Dispose()
    {
        _sGraphicsModule.WaitIdle();
        _rootView.Dispose();
    }

    public void Update(float deltaTime)
    {
        if (_isCursorIn) DoHover();
        _rootView.Update(deltaTime);
        ForceLayout(); // Settle layout before Collect so the render path never triggers lazy computation.
    }

    public abstract Vector2 GetCursorPosition();

    public abstract void SetCursorPosition(Vector2 position);

    public abstract void StartTyping(View view);
    public abstract void StopTyping(View view);
    public abstract void SetTypingRect(View view, in Rect2D caret);
    public event Action<CursorUpSurfaceEvent>? OnCursorUp;

    public virtual void Init()
    {
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

    /// <summary>
    ///     Where a command list from <see cref="CollectCommands" /> goes back once its frame is done with it.
    /// </summary>
    public Recycler<CommandList> CommandLists { get; } =
        new(() => new CommandList { SurfaceSize = Vector2.Zero });

    public CommandList? CollectCommands()
    {
        var size = GetSize();

        var drawList = CommandLists.Take();
        drawList.SurfaceSize = size;
        _rootView.Collect(_rootView.BoxToParent(), new Rect2D
        {
            Size = size
        }, drawList);

        if (drawList.Commands.Count != 0) return drawList;

        CommandLists.Return(drawList);
        return null;
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
        _rootView.Layout(e.Size);
    }

    public virtual void ReceiveCursorDown(CursorDownSurfaceEvent e)
    {
        _rootView.HandleEvent(e, _rootView.BoxToParent());
        if (e.Target is not null)
        {
            _lastCursorDownEvent = e;
            if (e.Target is { IsFocusable: true } target && FocusedView != target) RequestFocus(target);
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
        _rootView.HandleEvent(e, _rootView.BoxToParent());
        _lastHovered.AddRange(e.Over);
        _lastCursorDownEvent?.Target?.OnCursorMove(e, _lastCursorDownEvent.Target.ContentToSurface());
    }

    public virtual void ReceiveScroll(ScrollSurfaceEvent e)
    {
        _rootView.HandleEvent(e, _rootView.BoxToParent());
    }

    public virtual void ReceiveCharacter(CharacterSurfaceEvent e)
    {
        FocusedView?.OnCharacter(e);
    }

    public virtual void ReceiveTextPreedit(TextPreeditSurfaceEvent e)
    {
        FocusedView?.OnTextPreedit(e);
    }

    public virtual void ReceiveKeyboard(KeyboardSurfaceEvent e)
    {
        FocusedView?.OnKeyboard(e);
    }

    public virtual T Add<T>() where T : IView, new()
    {
        return Add(new T());
    }

    public virtual T Add<T>(T view) where T : IView
    {
        _rootView.Add(view);
        return view;
    }

    public virtual bool Remove(IView view)
    {
        return _rootView.Remove(view);
    }

    public void OnViewLayoutInvalidated(IView view)
    {
        _viewsPendingLayout.Add(view);
    }

    public void ForceLayout()
    {
        if (_isCalculatingLayout) return;
        _isCalculatingLayout = true;
        // Filter by surface: a view keeps its pending entry if moved to another surface mid-frame.
        // Order by depth: parents are processed first, so their Layout() pass covers children —
        // those children arrive at the loop below already valid and are skipped.
        var toLayout = _viewsPendingLayout.Where(c => c.Surface == this).OrderBy(c => c.Depth).ToArray();
        _viewsPendingLayout.Clear();
        foreach (var view in toLayout)
            if (!view.IsLayoutValid) // may already be valid if an ancestor's pass covered it
            {
                if (view.Parent is not null)
                    view.Parent.OnChildNeedsLayout(view);
                else
                    view.Layout(GetSize());
            }

        _isCalculatingLayout = false;
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
                _rootView.HandleEvent(e, _rootView.BoxToParent());
                _lastHovered.AddRange(e.Over);
            }
        }

        var hoveredSet = _lastHovered.ToHashSet();


        foreach (var view in oldHoverList.AsReversed())
            if (!hoveredSet.Contains(view))
                view.NotifyCursorLeave();
    }
}