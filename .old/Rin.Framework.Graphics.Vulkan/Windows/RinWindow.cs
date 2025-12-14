using System.Numerics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Graphics.Windows.Events;

namespace Rin.Framework.Graphics.Vulkan.Windows;

public class RinWindow(in ulong handle, IWindow? parent) : IWindow
{
    private readonly HashSet<IWindow> _children = [];
    private ulong _handle = handle;
    public IWindow? Parent { get; } = parent;

    public void Dispose()
    {
        foreach (var window in _children) window.Dispose();
        _children.Clear();

        OnDispose?.Invoke();
        if (_handle == 0) return;
        Native.Platform.Window.Destroy(_handle);
        _handle = 0;
    }

    public bool IsFullscreen { get; }
    public event Action<KeyEvent>? OnKey;
    public event Action<CursorMoveEvent>? OnCursorMoved;
    public event Action<CursorButtonEvent>? OnCursorButton;
    public event Action<FocusEvent>? OnCursorFocus;
    public event Action<FocusEvent>? OnKeyboardFocus;
    public event Action<ScrollEvent>? OnScroll;
    public event Action<ResizeEvent>? OnResize;
    public event Action<CloseEvent>? OnClose;
    public event Action<CharacterEvent>? OnCharacter;
    public event Action<MaximizeEvent>? OnMaximize;
    public event Action<MinimizeEvent>? OnMinimize;
    public event Action<RefreshEvent>? OnRefresh;
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

    public Extent2D GetSize()
    {
        return Native.Platform.Window.GetSize(_handle);
    }

    public IWindow CreateChild(string name, in Extent2D extent, WindowFlags flags = WindowFlags.Visible)
    {
        var child = IGraphicsModule.Get().CreateWindow(name, extent, flags, this);
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
                    Window = this
                });
                break;
            case Native.Platform.Window.EventType.Resize:
                OnResize?.Invoke(new ResizeEvent
                {
                    Window = this,
                    Size = e.resize.size
                });
                break;
            case Native.Platform.Window.EventType.Minimize:
                OnMinimize?.Invoke(new MinimizeEvent
                {
                    Window = this
                });
                break;
            case Native.Platform.Window.EventType.Maximize:
                OnMaximize?.Invoke(new MaximizeEvent
                {
                    Window = this
                });
                break;
            case Native.Platform.Window.EventType.Scroll:
                OnScroll?.Invoke(new ScrollEvent
                {
                    Window = this,
                    Delta = e.scroll.delta,
                    Position = GetCursorPosition()
                });
                break;
            case Native.Platform.Window.EventType.CursorMove:
                OnCursorMoved?.Invoke(new CursorMoveEvent
                {
                    Window = this,
                    Position = GetCursorPosition()
                });
                break;
            case Native.Platform.Window.EventType.CursorButton:
                OnCursorButton?.Invoke(new CursorButtonEvent
                {
                    Window = this,
                    Position = GetCursorPosition(),
                    Button = e.cursorButton.button,
                    Modifiers = e.cursorButton.modifier,
                    State = e.cursorButton.state
                });
                break;
            case Native.Platform.Window.EventType.CursorFocus:
                OnCursorFocus?.Invoke(new FocusEvent
                {
                    Window = this,
                    IsFocused = e.cursorFocus.focused == 1
                });
                break;
            case Native.Platform.Window.EventType.KeyboardFocus:
                OnKeyboardFocus?.Invoke(new FocusEvent
                {
                    Window = this,
                    IsFocused = e.keyboardFocus.focused == 1
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

    public void SetPosition(in Offset2D position)
    {
        throw new NotImplementedException();
    }

    public ulong GetHandle()
    {
        return _handle;
    }
}