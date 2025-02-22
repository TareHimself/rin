using System.Numerics;
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
    
    public event Action<CursorEvent>? OnCursorEnter;
    public event Action<WindowEvent>? OnCursorLeave;
    public event Action<FocusEvent>? OnFocused;
    public event Action<ScrollEvent>? OnScrolled;
    public event Action<ResizeEvent>? OnResized;
    public event Action<CloseEvent>? OnCloseRequested;
    public event Action<CharacterEvent>? OnCharacter;
    public event Action<MaximizedEvent>? OnMaximized;
    public event Action<RefreshEvent>? OnRefresh;
    public event Action<MinimizeEvent>? OnMinimized;
    public event Action<DropEvent>? OnDrop;

    public Vector2 GetCursorPosition();

    public void SetCursorPosition(Vector2 position);

    public void SetFullscreen(bool state);

    public Vector2<uint> GetPixelSize();

    public nuint GetPtr();

    public IWindow CreateChild(int width, int height, string name, CreateOptions? options = null);

    public event Action? OnDisposed;
}