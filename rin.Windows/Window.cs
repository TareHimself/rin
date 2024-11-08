using System.Runtime.InteropServices;
using rin.Core;
using rin.Core.Math;
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace rin.Windows;

/// <summary>
/// Abstraction for a <a href="https://www.glfw.org/">Glfw Window</a>
/// </summary>
public class Window : Disposable
{
    [Flags]
    public enum Mods
    {
        Shift = 0x0001,
        Control = 0x0002,
        Alt = 0x0004,
        Super = 0x0008,
        CapsLock = 0x0010,
        NumLock = 0x0020
    }
    
    private readonly nint _nativePtr;
    
    private readonly NativeMethods.NativeCharDelegate _charDelegate;

    private readonly NativeMethods.NativeCloseDelegate _closeDelegate;

    private readonly NativeMethods.NativeCursorDelegate _cursorDelegate;

    private readonly NativeMethods.NativeFocusDelegate _focusDelegate;

    private readonly NativeMethods.NativeKeyDelegate _keyDelegate;

    private readonly NativeMethods.NativeMouseButtonDelegate _mouseButtonDelegate;

    private readonly NativeMethods.NativeScrollDelegate _scrollDelegate;

    private readonly NativeMethods.NativeSizeDelegate _sizeDelegate;
    
    private readonly NativeMethods.NativeMaximizedDelegate _maximizedDelegate;
    
    private readonly NativeMethods.NativeRefreshDelegate _refreshDelegate;

    public readonly List<Window> Children = [];

    public readonly Window? Parent;

    private Vector2<double>? _lastCursorPosition;

    public bool Focused = true;


    public Vector2<uint> PixelSize;

    public Window(nint nativePtr, Window? parent)
    {
        Parent = parent;
        _keyDelegate = KeyCallback;
        _cursorDelegate = CursorCallback;
        _mouseButtonDelegate = MouseButtonCallback;
        _focusDelegate = FocusCallback;
        _scrollDelegate = ScrollCallback;
        _sizeDelegate = SizeCallback;
        _closeDelegate = CloseCallback;
        _charDelegate = CharCallback;
        _maximizedDelegate = MaximizedCallback;
        _refreshDelegate = RefreshCallback;
        _nativePtr = nativePtr;
        int width = 0, height = 0;
        NativeMethods.GetWindowPixelSize(nativePtr, ref width, ref height);
        PixelSize.X = (uint)width;
        PixelSize.Y = (uint)height;
        NativeMethods.SetWindowCallbacks(_nativePtr,
            _keyDelegate,
            _cursorDelegate,
            _mouseButtonDelegate,
            _focusDelegate,
            _scrollDelegate,
            _sizeDelegate,
            _closeDelegate,
            _charDelegate,
            _maximizedDelegate,
            _refreshDelegate);
    }

    public Window CreateChild(int width, int height, string name, WindowCreateOptions? options = null)
    {
        var child = SWindowsModule.Get().CreateWindow(width, height, name, this);
        Children.Add(child);
        return child;
    }


    public event Action? OnDisposed;

    private void KeyCallback(nint window, int key, int scancode, int action, int mods)
    {
        OnKey?.Invoke(new KeyEvent(this, key, action, (Mods)mods));
    }

    private void CursorCallback(nint window, double x, double y)
    {
        var position = new Vector2<double>(x, y);
        var delta = _lastCursorPosition == null ? new Vector2<double>(0, 0) : position - _lastCursorPosition.Value;
        _lastCursorPosition = position;
        OnMouseMoved?.Invoke(new MouseMoveEvent(this, position, delta));
    }

    private void MouseButtonCallback(nint window, int button, int action, int mods)
    {
        OnMouseButton?.Invoke(new MouseButtonEvent(this, button, action, (Mods)mods));
    }

    private void FocusCallback(nint window, int focused)
    {
        Focused = focused == 1;
        OnFocused?.Invoke(new FocusEvent(this, focused == 0));
    }

    private void ScrollCallback(nint window, double dx, double dy)
    {
        OnScrolled?.Invoke(new ScrollEvent(this, dx, dy));
    }

    private void SizeCallback(nint window, int eWidth, int eHeight)
    {
        PixelSize.X = (uint)eWidth;
        PixelSize.Y = (uint)eHeight;
        OnResized?.Invoke(new ResizeEvent(this, PixelSize.X, PixelSize.Y));
        AfterResize?.Invoke(new ResizeEvent(this, PixelSize.X, PixelSize.Y));
    }

    private void CloseCallback(nint window)
    {
        OnCloseRequested?.Invoke(new CloseEvent(this));
    }

    
    private void CharCallback(nint window, uint inCode, int inMods)
    {
        OnChar?.Invoke(new CharEvent(this, inCode, (Mods)inMods));
    }
    
    private void MaximizedCallback(nint window, int maxmized)
    {
        OnMaximized?.Invoke(new MaximizedEvent(this,maxmized == 1));
        var size = GetPixelSize().Cast<uint>();
        OnResized?.Invoke(new ResizeEvent(this,size.X,size.Y));
    }
    
    private void RefreshCallback(nint window)
    {
        OnRefresh?.Invoke(new RefreshEvent(this));
    }

    protected override void OnDispose(bool isManual)
    {
        foreach (var window in Children) window.Dispose();

        Children.Clear();

        OnDisposed?.Invoke();
    }

    

    public Vector2<double> GetMousePosition()
    {
        var x = 0.0;
        var y = 0.0;
        NativeMethods.GetWindowMousePosition(_nativePtr, ref x, ref y);

        return new Vector2<double>(x, y);
    }
    
    public void SetMousePosition(Vector2<double> position)
    {
        NativeMethods.SetWindowMousePosition(_nativePtr,position.X,position.Y);
    }


    public Vector2<int> GetPixelSize()
    {
        Vector2<int> result = 0;
        NativeMethods.GetWindowPixelSize(_nativePtr,ref result.X,ref result.Y);
        return result;
    }


    public event Action<KeyEvent>? OnKey;
    public event Action<MouseMoveEvent>? OnMouseMoved;
    public event Action<MouseButtonEvent>? OnMouseButton;
    public event Action<FocusEvent>? OnFocused;
    public event Action<ScrollEvent>? OnScrolled;
    public event Action<ResizeEvent>? OnResized;
    public event Action<ResizeEvent>? AfterResize;
    public event Action<CloseEvent>? OnCloseRequested;
    public event Action<CharEvent>? OnChar;
    
    public event Action<MaximizedEvent>? OnMaximized;
    
    public event Action<RefreshEvent>? OnRefresh;

    public nint GetPtr()
    {
        return _nativePtr;
    }


    public class Event
    {
        public Window Window;

        protected Event(Window inWindow)
        {
            Window = inWindow;
        }
    }

    public class KeyEvent(Window inWindow, int inKey, int inAction, Mods mods)
        : Event(inWindow)
    {
        public readonly Mods Mods = mods;
        public readonly bool IsAltDown = mods.HasFlag(Mods.Alt);
        public readonly bool IsControlDown = mods.HasFlag(Mods.Control);
        public readonly bool IsShiftDown = mods.HasFlag(Mods.Shift);
        public readonly Key Key = (Key)inKey;
        public readonly KeyState State = (KeyState)inAction;
    }

    public class MouseMoveEvent(Window inWindow, Vector2<double> inPosition, Vector2<double> inDelta)
        : Event(inWindow)
    {
        public readonly Vector2<double> Delta = inDelta;
        public readonly Vector2<double> Position = inPosition;
    }

    public class MouseButtonEvent(Window inWindow, int button, int inAction, Mods mods)
        : Event(inWindow)
    {
        public readonly Vector2<double> Position = inWindow.GetMousePosition();
        public readonly KeyState State = (KeyState)inAction;
        public readonly MouseButton Button = (MouseButton)button;
        public readonly Mods Mods = mods;
        public readonly bool IsAltDown = mods.HasFlag(Mods.Alt);
        public readonly bool IsControlDown = mods.HasFlag(Mods.Control);
        public readonly bool IsShiftDown = mods.HasFlag(Mods.Shift);
    }

    public class FocusEvent(Window inWindow, bool inFocused) : Event(inWindow)
    {
        public bool Focused = inFocused;
    }

    public class ScrollEvent(Window inWindow, double inDx, double inDy) : Event(inWindow)
    {
        public readonly Vector2<double> Delta = new(inDx, inDy);
        public readonly Vector2<double> Position = inWindow.GetMousePosition();
    }

    public class ResizeEvent(Window inWindow, uint inWidth, uint inHeight) : Event(inWindow)
    {
        public readonly uint Height = inHeight;
        public readonly uint Width = inWidth;
    }

    public class CloseEvent(Window inWindow) : Event(inWindow);

    public class CharEvent(Window inWindow, uint inCode, Mods inMods) : Event(inWindow)
    {
        public readonly char Data = (char)inCode;
        public Mods Mods = inMods;
    }
    
    public class MaximizedEvent(Window inWindow, bool maximized) : Event(inWindow)
    {
        public readonly bool Maximized = maximized;
    }
    
    public class RefreshEvent(Window inWindow) : Event(inWindow)
    {
    }

    
}