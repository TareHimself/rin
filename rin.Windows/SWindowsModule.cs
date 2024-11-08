using System.Runtime.InteropServices;
using rin.Core;

namespace rin.Windows;

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

   

    public Window CreateWindow(int width, int height, string name, Window? parent = null,
        WindowCreateOptions? options = null)
    {
        var opts = options ?? new WindowCreateOptions();
        var winPtr = NativeMethods.Create(width, height, name, ref opts);

        var win = new Window(winPtr, parent);

        win.OnDisposed += () =>
        {
            OnWindowClosed?.Invoke(win);
            NativeMethods.Destroy(winPtr);
        };

        OnWindowCreated?.Invoke(win);

        return win;
    }


    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);

        NativeMethods.Start();

        runtime.OnTick += delta => { NativeMethods.PollEvents(); };
    }


    public override void Shutdown(SRuntime runtime)
    {
        base.Shutdown(runtime);
        NativeMethods.Stop();
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