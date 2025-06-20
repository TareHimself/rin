using System.Numerics;
using Rin.Engine.Graphics.Windows.Events;

namespace Rin.Engine.Graphics.Windows;

public interface IWindow : IDisposable
{
    public IWindow? Parent { get; }
    
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
    public event Action<RefreshEvent>? OnRefresh;
    public event Action<MinimizeEvent>? OnMinimize;
    public event Action<DropEvent>? OnDrop;

    public void SetHitTestCallback(Func<WindowHitTestResult, IWindow>? callback);

    /// <summary>
    ///     Gets the position of the cursor relative to the drawable area of this window
    /// </summary>
    /// <returns></returns>
    public Vector2 GetCursorPosition();

    /// <summary>
    ///     Sets the position of the cursor relative to the drawable area of this window
    /// </summary>
    /// <param name="position"></param>
    public void SetCursorPosition(in Vector2 position);


    public void SetFullscreen(bool state);

    /// <summary>
    ///     Sets the size of this window
    /// </summary>
    /// <param name="size"></param>
    public void SetSize(in Extent2D size);

    /// <summary>
    ///     Gets the rect for the drawable area of this window
    /// </summary>
    /// <returns></returns>
    public Extent2D GetSize();

    public IWindow CreateChild(int width, int height, string name, WindowFlags flags = WindowFlags.Visible);

    public void StartTyping();

    public void StopTyping();

    public event Action? OnDispose;
}