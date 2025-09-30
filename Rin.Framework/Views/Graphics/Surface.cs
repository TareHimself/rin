using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Extensions;
using Rin.Framework.Math;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;
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
    private readonly RootView _rootView = new();
    private readonly IGraphicsModule _sGraphicsModule;
    private bool _isCursorIn;
    private CursorDownSurfaceEvent? _lastCursorDownEvent;

    protected Surface()
    {
        _sGraphicsModule = IGraphicsModule.Get();
        _rootView.NotifyAddedToSurface(this);
    }

    public IView? FocusedView { get; private set; }

    public virtual void Dispose()
    {
        _sGraphicsModule.WaitIdle();
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
    
    public CommandList? CollectCommands()
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

        return drawList;
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

    public virtual T Add<T>() where T : IView, new()
    {
        return Add(Activator.CreateInstance<T>());
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
}