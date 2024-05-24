using System.Runtime.InteropServices;

namespace aerox.Runtime.Windows;

/// <summary>
///     Manages all <see cref="Window">Windows</see>
/// </summary>
[NativeRuntimeModule]
public class WindowsModule : RuntimeModule,ISingletonGetter<WindowsModule>
{
    private const string DllPath = "aerox_native.dll";

    public event Action<Window> OnWindowClosed;


    public event Action<Window> OnWindowCreated;

    [DllImport(Dlls.AeroxNative, EntryPoint = "windowCreate", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint NativeCreate(int width, int height, string name, IntPtr parent);

    [DllImport(Dlls.AeroxNative, EntryPoint = "windowDestroy", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint NativeDestroy(nint nativePtr);

    [DllImport(Dlls.AeroxNative, EntryPoint = "windowSubsystemStart", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool NativeStart();

    [DllImport(Dlls.AeroxNative, EntryPoint = "windowSubsystemStop", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeStop();

    [DllImport(Dlls.AeroxNative, EntryPoint = "windowSubsystemPollEvents", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativePollEvents();
    public Window CreateWindow(int width, int height, string name, Window? parent = null)
    {
        var winPtr = NativeCreate(width, height, name,IntPtr.Zero);

        var win = new Window(winPtr,parent);

        win.OnDisposed += () =>
        {
            OnWindowClosed?.Invoke(win);
            NativeDestroy(winPtr);
        };

        OnWindowCreated?.Invoke(win);

        return win;
    }


    public override void Startup(Runtime runtime)
    {
        base.Startup(runtime);

        NativeStart();

        runtime.OnTick += delta => { NativePollEvents(); };
    }


    public override void Shutdown(Runtime runtime)
    {
        base.Shutdown(runtime);
        NativeStop();
    }

    public class WindowCreatedEvent
    {
        public Window window;

        protected WindowCreatedEvent(Window inWindow)
        {
            window = inWindow;
        }
    }

    public class WindowClosedEvent
    {
        public Window window;

        protected WindowClosedEvent(Window inWindow)
        {
            window = inWindow;
        }
    }

    public static WindowsModule Get()
    {
        return Runtime.Instance.GetModule<WindowsModule>();
    }
}