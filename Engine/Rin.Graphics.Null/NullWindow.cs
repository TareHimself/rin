using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Graphics.Windows.Events;

namespace Rin.Graphics.Null;

internal sealed class NullWindow(string name, Extent2D extent, IWindow? parent) : IWindow
{
    private Extent2D _size = extent;

    public string Name => name;
    public IWindow? Parent => parent;
    public bool IsFullscreen { get; private set; }

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
    public event Action<RefreshEvent>? OnRefresh;
    public event Action<MinimizeEvent>? OnMinimize;
    public event Action<DropEvent>? OnDrop;
    public event Action? OnDispose;

    public void SetHitTestCallback(Func<WindowHitTestResult, IWindow>? callback)
    {
    }

    public Vector2 GetCursorPosition()
    {
        return Vector2.Zero;
    }

    public void SetCursorPosition(in Vector2 position)
    {
    }

    public void SetFullscreen(bool state)
    {
        IsFullscreen = state;
    }

    public void SetSize(in Extent2D size)
    {
        _size = size;
    }

    public Extent2D GetSize()
    {
        return _size;
    }

    public IWindow CreateChild(string childName, in Extent2D childExtent, WindowFlags flags = WindowFlags.Visible)
    {
        return new NullWindow(childName, childExtent, this);
    }

    public void StartTyping()
    {
    }

    public void StopTyping()
    {
    }

    public void Dispose()
    {
        OnDispose?.Invoke();
    }
}
