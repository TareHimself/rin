using System.Runtime.InteropServices;
using aerox.Runtime.Math;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Windows;

/// <summary>
///     Abstraction for a <a href="https://www.glfw.org/">Glfw Window</a>
/// </summary>
public class Window : Disposable
{
    [Flags]
    public enum Mods
    {
        ModShift = 0x0001,
        ModControl = 0x0002,
        ModAlt = 0x0004,
        ModSuper = 0x0008,
        ModCapsLock = 0x0010,
        ModNumLock = 0x0020
    }

    private readonly NativeCharDelegate _charDelegate;

    private readonly NativeCloseDelegate _closeDelegate;

    private readonly NativeCursorDelegate _cursorDelegate;

    private readonly NativeFocusDelegate _focusDelegate;

    private readonly NativeKeyDelegate _keyDelegate;

    private readonly NativeMouseButtonDelegate _mouseButtonDelegate;

    private readonly nint _nativePtr;

    private readonly NativeScrollDelegate _scrollDelegate;

    private readonly NativeSizeDelegate _sizeDelegate;

    private Vector2<double>? _lastCursorPositon;

    public bool Focused = true;


    public VkExtent2D PixelSize;

    public readonly Window? Parent;

    public Window(nint nativePtr,Window? parent)
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
        _nativePtr = nativePtr;
        int width = 0, height = 0;
        NativeGetPixelSize(nativePtr, ref width, ref height);
        PixelSize.width = (uint)width;
        PixelSize.height = (uint)height;
        NativeSetWindowCallbacks(_nativePtr,
            _keyDelegate,
            _cursorDelegate,
            _mouseButtonDelegate,
            _focusDelegate,
            _scrollDelegate,
            _sizeDelegate,
            _closeDelegate,
            _charDelegate);
    }

    public readonly List<Window> Children = new();

    public Window CreateChild(int width, int height, string name)
    {
        var child = WindowsModule.Get().CreateWindow(width, height, name, this);
        Children.Add(child);
        return child;
    }


    public event Action? OnDisposed;

    private void KeyCallback(nint window, int key, int scancode, int action, int mods)
    {
        OnKey?.Invoke(new KeyEvent(this, key, action, mods));
    }

    private void CursorCallback(nint window, double x, double y)
    {
        var position = new Vector2<double>(x, y);
        var delta = _lastCursorPositon == null ? new Vector2<double>(0, 0) : position - _lastCursorPositon.Value;
        _lastCursorPositon = position;
        OnMouseMoved?.Invoke(new MouseMoveEvent(this, position, delta));
    }

    private void MouseButtonCallback(nint window, int button, int action, int mods)
    {
        OnMouseButton?.Invoke(new MouseButtonEvent(this, button, action, mods));
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
        
        PixelSize.width = (uint)eWidth;
        PixelSize.height = (uint)eHeight;
        OnResized?.Invoke(new ResizeEvent(this, PixelSize.width, PixelSize.height));
        AfterResize?.Invoke(new ResizeEvent(this, PixelSize.width, PixelSize.height));
    }

    private void CloseCallback(nint window)
    {
        OnCloseRequested?.Invoke(new CloseEvent(this));
    }

    private void CharCallback(nint window, uint inCode, int inMods)
    {
        OnChar?.Invoke(new CharEvent(this, inCode, inMods));
    }

    protected override void OnDispose(bool isManual)
    {
        foreach (var window in Children)
        {
            window.Dispose();
        }
        
        Children.Clear();

        OnDisposed?.Invoke();
    }

    [DllImport(Dlls.AeroxNative, EntryPoint = "windowGetMousePosition", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeGetMousePosition(nint window, ref double x,ref double y);

    [DllImport(Dlls.AeroxNative, EntryPoint = "windowGetPixelSize", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeGetPixelSize(nint window, ref int width, ref int height);

    public Vector2<double> GetMousePosition()
    {
        var x = 0.0;
        var y = 0.0;
        NativeGetMousePosition(_nativePtr, ref x, ref y);

        return new Vector2<double>(x, y);
    }


    [DllImport(Dlls.AeroxNative, EntryPoint = "windowSetCallbacks", CallingConvention = CallingConvention.Cdecl)]
    protected static extern void NativeSetWindowCallbacks(nint nativePtr,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeKeyDelegate keyDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeCursorDelegate cursorDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeMouseButtonDelegate mouseButtonDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeFocusDelegate focusDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeScrollDelegate scrollDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeSizeDelegate sizeDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeCloseDelegate closeDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeCharDelegate charDelegate);


    public event Action<KeyEvent>? OnKey;
    public event Action<MouseMoveEvent>? OnMouseMoved;
    public event Action<MouseButtonEvent>? OnMouseButton;
    public event Action<FocusEvent>? OnFocused;
    public event Action<ScrollEvent>? OnScrolled;
    public event Action<ResizeEvent>? OnResized;
    public event Action<ResizeEvent>? AfterResize;
    public event Action<CloseEvent>? OnCloseRequested;
    public event Action<CharEvent>? OnChar;

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

    public class KeyEvent(Window inWindow, int inKey, int inAction, int inMods)
        : Event(inWindow)
    {
        public readonly bool IsAltDown = (inMods & (int)Mods.ModAlt) == (int)Mods.ModAlt;
        public readonly bool IsControlDown = (inMods & (int)Mods.ModControl) == (int)Mods.ModControl;

        public readonly bool IsShiftDown = (inMods & (int)Mods.ModShift) == (int)Mods.ModShift;
        public readonly EKey Key = (EKey)inKey;
        public readonly EKeyState State = (EKeyState)inAction;
    }

    public class MouseMoveEvent(Window inWindow, Vector2<double> inPosition, Vector2<double> inDelta)
        : Event(inWindow)
    {
        public readonly Vector2<double> Delta = inDelta;
        public readonly Vector2<double> Position = inPosition;
    }

    public class MouseButtonEvent : Event
    {
        public readonly EKeyState State;
        public readonly Vector2<double> Position;

        public MouseButtonEvent(Window inWindow, int button, int inAction, int mods) : base(inWindow)
        {
            Position = inWindow.GetMousePosition();
            State = (EKeyState)inAction;
        }
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

    public class CharEvent(Window inWindow, uint inCode, int inMods) : Event(inWindow)
    {
        public readonly char Data = (char)inCode;
        public int Mods = inMods;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    protected delegate void NativeKeyDelegate(nint window, int key, int scancode, int action, int mods);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    protected delegate void NativeCursorDelegate(nint window, double x, double y);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    protected delegate void NativeMouseButtonDelegate(nint window, int button, int action, int mods);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    protected delegate void NativeFocusDelegate(nint window, int focused);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    protected delegate void NativeScrollDelegate(nint window, double dx, double dy);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    protected delegate void NativeSizeDelegate(nint window, int width, int height);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    protected delegate void NativeCloseDelegate(nint window);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    protected delegate void NativeCharDelegate(nint window, uint code, int mods);
}