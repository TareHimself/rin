using System.Runtime.InteropServices;

namespace rin.Windows;

public static class NativeMethods
{
    private const string DllName = "rin.WindowsN";
    
    [DllImport(DllName, EntryPoint = "windowGetMousePosition", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetWindowMousePosition(IntPtr window, ref double x, ref double y);
    
    [DllImport(DllName, EntryPoint = "windowSetMousePosition", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetWindowMousePosition(IntPtr window,double x,double y);

    [DllImport(DllName, EntryPoint = "windowGetPixelSize", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetWindowPixelSize(IntPtr window, ref int width, ref int height);
    
    [DllImport(DllName, EntryPoint = "windowCreate", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint Create(int width, int height, string name, ref WindowCreateOptions options);

    [DllImport(DllName, EntryPoint = "windowDestroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint Destroy(IntPtr nativePtr);

    [DllImport(DllName, EntryPoint = "windowSubsystemStart", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool Start();

    [DllImport(DllName, EntryPoint = "windowSubsystemStop", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Stop();

    [DllImport(DllName, EntryPoint = "windowSubsystemPollEvents", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PollEvents();
    
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeKeyDelegate(nint window, int key, int scancode, int action, int mods);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeCursorDelegate(nint window, double x, double y);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeMouseButtonDelegate(nint window, int button, int action, int mods);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeFocusDelegate(nint window, int focused);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeScrollDelegate(nint window, double dx, double dy);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeSizeDelegate(nint window, int width, int height);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeCloseDelegate(nint window);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeCharDelegate(nint window, uint code, int mods);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeMaximizedDelegate(nint window, int maximized);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeRefreshDelegate(nint window);
    
    [DllImport(DllName, EntryPoint = "windowSetCallbacks", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetWindowCallbacks(nint nativePtr,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeKeyDelegate keyDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeCursorDelegate cursorDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeMouseButtonDelegate mouseButtonDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeFocusDelegate focusDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeScrollDelegate scrollDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeSizeDelegate sizeDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeCloseDelegate closeDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeCharDelegate charDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeMaximizedDelegate maximizedDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeRefreshDelegate refreshDelegate);
}