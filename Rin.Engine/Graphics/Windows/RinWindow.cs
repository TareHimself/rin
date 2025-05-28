using System.Numerics;
using Rin.Engine.Graphics.Windows.Events;
using Rin.Engine.Math;

namespace Rin.Engine.Graphics.Windows;

public class RinWindow(in IntPtr handle, IWindow? parent) : IWindow
{
    private readonly HashSet<IWindow> _children = [];
    private IntPtr _handle = handle;
    public IWindow? Parent { get; } = parent;

    internal void ProcessEvent(in Native.Platform.Window.WindowEvent e)
    {
        switch (e.info.type)
        {
            case Native.Platform.Window.EventType.Key:
                OnKey?.Invoke(new KeyEvent
                {
                    Key = e.key.key,
                    Modifiers = e.key.modifier,
                    State = e.key.state,
                    Window = this,
                });
                break;
            case Native.Platform.Window.EventType.Resize:
                OnResize?.Invoke(new ResizeEvent
                {
                    Window = this,
                    Rect = e.resize.rect,
                    DrawRect = e.resize.drawRect,
                });
                break;
            case Native.Platform.Window.EventType.Minimize:
                OnMinimize?.Invoke(new MinimizeEvent
                {
                    Window = this,
                });
                break;
            case Native.Platform.Window.EventType.Maximize:
                OnMaximize?.Invoke(new MaximizeEvent
                {
                    Window = this,
                });
                break;
            case Native.Platform.Window.EventType.Scroll:
                OnScroll?.Invoke(new ScrollEvent
                {
                    Window = this,
                    Delta = new Vector2(e.scroll.dx,e.scroll.dy),
                    Position = GetCursorPosition()
                });
                break;
            case Native.Platform.Window.EventType.CursorMove:
                OnCursorEnter?.Invoke(new CursorMoveEvent
                {
                    Window = this,
                    Position = GetCursorPosition()
                });
                break;
            case Native.Platform.Window.EventType.CursorButton:
                break;
            case Native.Platform.Window.EventType.CursorEnter:
                OnCursorEnter?.Invoke(new CursorEvent
                {
                    Window = this,
                    Position = GetCursorPosition()
                });
                break;
            case Native.Platform.Window.EventType.CursorLeave:
                OnCursorLeave?.Invoke(new CursorEvent
                {
                    Window = this,
                    Position = GetCursorPosition()
                });
                break;
            case Native.Platform.Window.EventType.Focus:
                OnFocus?.Invoke(new FocusEvent
                {
                    Window = this,
                    IsFocused = e.focus.focused == 1,
                });
                break;
            case Native.Platform.Window.EventType.Close:
                OnClose?.Invoke(new CloseEvent
                {
                    Window = this
                });
                break;
            case Native.Platform.Window.EventType.Text:
                OnCharacter?.Invoke(new CharacterEvent
                {
                    Window = this,
                    Data = e.text.text,
                    Modifiers = 0
                });
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    public void Dispose()
    {
        foreach (var window in _children) window.Dispose();
        _children.Clear();
        
        OnDispose?.Invoke();
        if (_handle == IntPtr.Zero) return;
        Native.Platform.Window.Destroy(_handle);
        _handle = IntPtr.Zero;
    }

    
    public bool Focused { get; }
    public bool IsFullscreen { get; }
    public event Action<KeyEvent>? OnKey;
    public event Action<CursorMoveEvent>? OnCursorMoved;
    public event Action<CursorButtonEvent>? OnCursorButton;
    public event Action<CursorEvent>? OnCursorEnter;
    public event Action<WindowEvent>? OnCursorLeave;
    public event Action<FocusEvent>? OnFocus;
    public event Action<ScrollEvent>? OnScroll;
    public event Action<ResizeEvent>? OnResize;
    public event Action<CloseEvent>? OnClose;
    public event Action<CharacterEvent>? OnCharacter;
    public event Action<MaximizeEvent>? OnMaximize;
    public event Action<RefreshEvent>? OnRefresh;
    public event Action<MinimizeEvent>? OnMinimize;
    public event Action<DropEvent>? OnDrop;
    public void SetHitTestCallback(Func<WindowHitTestResult, IWindow>? callback)
    {
        throw new NotImplementedException();
    }

    public Vector2 GetCursorPosition()
    {
        return Native.Platform.Window.GetCursorPosition(_handle);
    }

    public void SetCursorPosition(in Vector2 position)
    {
        Native.Platform.Window.SetCursorPosition(_handle, position);
    }

    public void SetFullscreen(bool state)
    {
        throw new NotImplementedException();
    }

    public void SetSize(in Extent2D size)
    {
        throw new NotImplementedException();
    }

    public void SetPosition(in Offset2D position)
    {
        throw new NotImplementedException();
    }

    public WindowRect GetDrawRect()
    {
        return Native.Platform.Window.GetDrawRect(_handle);
    }

    public WindowRect GetRect()
    {
        return Native.Platform.Window.GetRect(_handle);
    }
    
    public IntPtr GetHandle() => _handle;

    public IWindow CreateChild(int width, int height, string name, WindowFlags flags = WindowFlags.Visible)
    {
        var child = SGraphicsModule.Get().CreateWindow(width, height, name, flags,this);
        _children.Add(child);
        child.OnDispose += () => _children.Remove(child);
        return child;
    }

    public void StartTyping()
    {
        //throw new NotImplementedException();
    }

    public void StopTyping()
    {
        //throw new NotImplementedException();
    }

    public event Action? OnDispose;
}