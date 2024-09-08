using System.Runtime.InteropServices;

namespace aerox.Runtime.Windows;

/// <summary>
///     Manages all <see cref="Window">Windows</see>
/// </summary>
[NativeRuntimeModule]
public class SWindowsModule : RuntimeModule, ISingletonGetter<SWindowsModule>
{
    public static SWindowsModule Get()
    {
        return SRuntime.Get().GetModule<SWindowsModule>();
    }

    public event Action<Window>? OnWindowClosed;


    public event Action<Window>? OnWindowCreated;

    [DllImport(Dlls.AeroxWindowsNative, EntryPoint = "windowCreate", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint NativeCreate(int width, int height, string name, ref WindowCreateOptions options);

    [DllImport(Dlls.AeroxWindowsNative, EntryPoint = "windowDestroy", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint NativeDestroy(nint nativePtr);

    [DllImport(Dlls.AeroxWindowsNative, EntryPoint = "windowSubsystemStart", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool NativeStart();

    [DllImport(Dlls.AeroxWindowsNative, EntryPoint = "windowSubsystemStop", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeStop();

    [DllImport(Dlls.AeroxWindowsNative, EntryPoint = "windowSubsystemPollEvents", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativePollEvents();

    public Window CreateWindow(int width, int height, string name, Window? parent = null,
        WindowCreateOptions? options = null)
    {
        var opts = options ?? new WindowCreateOptions();
        var winPtr = NativeCreate(width, height, name, ref opts);

        var win = new Window(winPtr, parent);

        win.OnDisposed += () =>
        {
            OnWindowClosed?.Invoke(win);
            NativeDestroy(winPtr);
        };

        OnWindowCreated?.Invoke(win);

        return win;
    }


    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);

        NativeStart();

        runtime.OnTick += delta => { NativePollEvents(); };
    }


    public override void Shutdown(SRuntime runtime)
    {
        base.Shutdown(runtime);
        NativeStop();
    }

    public class WindowCreatedEvent
    {
        public Window Window;

        protected WindowCreatedEvent(Window inWindow)
        {
            Window = inWindow;
        }
    }

    public class WindowClosedEvent
    {
        public Window Window;

        protected WindowClosedEvent(Window inWindow)
        {
            Window = inWindow;
        }
    }
}