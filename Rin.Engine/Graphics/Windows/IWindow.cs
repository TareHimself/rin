using System.Numerics;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics.Windows.Events;

namespace Rin.Engine.Graphics.Windows;

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

    public void SetSize(int width, int height);

    public void SetPosition(int x, int y);

    public Vector2<uint> GetPixelSize();

    public nuint GetPtr();

    public IWindow CreateChild(int width, int height, string name, CreateOptions? options = null);

    public void StartTyping();

    public void StopTyping();

    public event Action? OnDisposed;
}