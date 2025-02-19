using rin.Framework.Core.Math;
using rin.Framework.Graphics.Windows.Events;

namespace rin.Framework.Graphics.Windows;

public interface IWindow : IDisposable
{
    public IWindow? Parent { get; }

    public bool Focused { get; }

    public bool IsFullscreen { get; }
    public event Action<KeyEvent>? OnKey;
    public event Action<CursorMoveEvent>? OnCursorMoved;
    public event Action<CursorButtonEvent>? OnCursorButton;
    public event Action<FocusEvent>? OnFocused;
    public event Action<ScrollEvent>? OnScrolled;
    public event Action<ResizeEvent>? OnResized;
    public event Action<ResizeEvent>? OnAfterResize;
    public event Action<CloseEvent>? OnCloseRequested;
    public event Action<CharacterEvent>? OnCharacter;
    public event Action<MaximizedEvent>? OnMaximized;
    public event Action<RefreshEvent>? OnRefresh;
    public event Action<MinimizeEvent>? OnMinimize;
    public event Action<DropEvent>? OnDrop;

    public Vec2<double> GetCursorPosition();

    public void SetMousePosition(Vec2<double> position);

    public void SetFullscreen(bool state);

    public Vec2<uint> GetPixelSize();

    public nint GetPtr();

    public IWindow CreateChild(int width, int height, string name, CreateOptions? options = null);

    public event Action? OnDisposed;
}