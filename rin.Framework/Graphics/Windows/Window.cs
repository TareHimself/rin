using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Graphics.Windows.Events;

namespace rin.Framework.Graphics.Windows;

public class Window : Disposable, IWindow
{
    private readonly nint _nativePtr;

    private readonly HashSet<IWindow> _children = [];

    public IWindow? Parent { get; private set;}

    private Vec2<double>? _lastCursorPosition;

    public bool Focused { get; private set; }

    public bool IsFullscreen => NativeMethods.GetWindowFullscreen(_nativePtr) == 1;

    public Vec2<uint> PixelSize;
    
    public event Action? OnDisposed;
    
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeMethods.NativeCharDelegate _charDelegate;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeMethods.NativeCloseDelegate _closeDelegate;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeMethods.NativeCursorDelegate _cursorDelegate;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeMethods.NativeFocusDelegate _focusDelegate;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeMethods.NativeKeyDelegate _keyDelegate;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeMethods.NativeMouseButtonDelegate _mouseButtonDelegate;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeMethods.NativeScrollDelegate _scrollDelegate;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeMethods.NativeSizeDelegate _sizeDelegate;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeMethods.NativeMaximizedDelegate _maximizedDelegate;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeMethods.NativeRefreshDelegate _refreshDelegate;
    
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeMethods.NativeMinimizeDelegate _minimizeDelegate;
    
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeMethods.NativeDropDelegate _dropDelegate;

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

    public Window(nint nativePtr, IWindow? parent)
    {
        unsafe
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
            _minimizeDelegate = MinimizeCallback;
            _dropDelegate = DropCallback;
            _nativePtr = nativePtr;
            PixelSize = GetPixelSize();
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
                _refreshDelegate,
                _minimizeDelegate,
                _dropDelegate);
        }
    }
    
    private void KeyCallback(nint window, int key, int scancode, int action, int mods)
    {
        OnKey?.Invoke(new KeyEvent
        {
            Window = this,
            Key = (InputKey)key,
            Modifiers = (InputModifier)mods,
            State = (InputState)action,
        });
    }

    private void CursorCallback(nint window, double x, double y)
    {
        var position = new Vec2<double>(x, y);
        var delta = _lastCursorPosition == null ? new Vec2<double>(0, 0) : position - _lastCursorPosition.Value;
        _lastCursorPosition = position;
        OnCursorMoved?.Invoke(new CursorMoveEvent
        {
            Window = this,
            Position = position.ToNumericsVector(),
            Delta = delta
        });
    }

    private void MouseButtonCallback(nint window, int button, int action, int mods)
    {
        OnCursorButton?.Invoke(new CursorButtonEvent
        {
            Window = this,
            Position = GetCursorPosition().ToNumericsVector(),
            Button = (CursorButton)button,
            Modifiers = (InputModifier)mods,
            State = (InputState)action,
        });
    }

    private void FocusCallback(nint window, int focused)
    {
        Focused = focused == 1;
        OnFocused?.Invoke(new FocusEvent
        {
            Window = this,
            IsFocused = focused == 1
        });
    }

    private void ScrollCallback(nint window, double dx, double dy)
    {
        OnScrolled?.Invoke(new ScrollEvent
        {
            Window = this,
            Position = GetCursorPosition().ToNumericsVector(),
            Delta = new Vec2<double>(dx, dy),
        });
    }

    private void SizeCallback(nint window, int eWidth, int eHeight)
    {
        PixelSize.X = (uint)eWidth;
        PixelSize.Y = (uint)eHeight;
        OnResized?.Invoke(new ResizeEvent
        {
            Window = this,
            Size = PixelSize.Clone()
        });
    }

    private void CloseCallback(nint window)
    {
        OnCloseRequested?.Invoke(new CloseEvent
        {
            Window = this
        });
    }

    
    private void CharCallback(nint window, uint inCode, int inMods)
    {
        OnCharacter?.Invoke(new CharacterEvent
        {
            Window = this,
            Data = (char)inCode,
            Modifiers = (InputModifier)inMods
        });
    }
    
    private void MaximizedCallback(nint window, int maxmized)
    {
        OnMaximized?.Invoke(new MaximizedEvent
        {
            Window = this,
            Maximized = maxmized == 1
        });
        var size = GetPixelSize().Cast<uint>().Cast<int>();
        SizeCallback(window,size.X,size.Y);
    }
    
    private void RefreshCallback(nint window)
    {
        OnRefresh?.Invoke(new RefreshEvent
        {
            Window = this
        });
    }
    
    private void MinimizeCallback(nint window,int minimized)
    {
        OnMinimize?.Invoke(new MinimizeEvent
        {
            Window = this,
            Minimized = minimized == 1
        });
        var size = GetPixelSize().Cast<uint>().Cast<int>();
        SizeCallback(window,size.X,size.Y);
    }
    
    private unsafe void DropCallback(nint window,int count,char ** paths)
    {

        OnDrop?.Invoke(new DropEvent
        {
            Window = this,
            Paths = Enumerable.Range(0, count).Select(c => Marshal.PtrToStringAnsi(new IntPtr(paths[c])) ?? "").ToArray()
        });
    }
    
    public Vec2<double> GetCursorPosition()
    {
        var x = 0.0;
        var y = 0.0;
        unsafe
        {
            NativeMethods.GetMousePosition(_nativePtr,&x,&y);
        }

        return new Vec2<double>(x, y);
    }
    
    public void SetMousePosition(Vec2<double> position)
    {
        NativeMethods.SetMousePosition(_nativePtr,position.X,position.Y);
    }

    public void SetFullscreen(bool state)
    {
        NativeMethods.SetWindowFullscreen(_nativePtr,state ? 1 : 0);
    }


    public Vec2<uint> GetPixelSize()
    {
        Vec2<int> result = 0;
        unsafe
        {
            NativeMethods.GetWindowPixelSize(_nativePtr,&result.X,&result.Y);
        }
        return result.Cast<uint>();
    }

    public nint GetPtr() => _nativePtr;

    protected override void OnDispose(bool isManual)
    {
        foreach (var window in _children) window.Dispose();

        _children.Clear();

        OnDisposed?.Invoke();
    }
    
    public IWindow CreateChild(int width, int height, string name, CreateOptions? options = null)
    {
        var child = SGraphicsModule.Get().CreateWindow(width, height, name, options,this);
        _children.Add(child);
        child.OnDisposed += () => _children.Remove(child);
        return child;
    }
}