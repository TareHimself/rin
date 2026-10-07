using Examples.Common;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Sources;
using Rin.Core.Views;
using Rin.Core.Views.Window;

namespace Examples;

public sealed class ExampleHost(Example? example, IReadOnlyList<Example> examples, string[] args) : ExampleApplication
{
    private static readonly Extent2D LauncherSize = new(1280, 800);

    private Example? _running;
    private bool _hasSurface;

    protected override void OnStartup()
    {
        Global.Sources.AddSource(AssemblyContentResource.New<ExampleHost>("Shaders/Examples"));

        IViewsModule.Get().OnSurfaceCreated += OnSurfaceCreated;
        IGraphicsModule.Get().OnWindowCreated += OnWindowCreated;
        IGraphicsModule.Get().CreateWindow(example?.Title ?? "Rin Examples", example?.WindowSize ?? LauncherSize,
            WindowFlags.Visible | WindowFlags.Resizable);
    }

    protected override void OnShutdown()
    {
        _running?.Stop();
        base.OnShutdown();
    }

    private void OnWindowCreated(IWindow window)
    {
        window.OnClose += _ =>
        {
            if (window.Parent != null)
                window.Dispose();
            else
                RequestExit();
        };
    }

    private void OnSurfaceCreated(IWindowSurface surface)
    {
        // Child windows get surfaces too, and the example belongs to the first one only.
        if (_hasSurface) return;
        _hasSurface = true;

        if (example is not null)
        {
            Run(example, surface);
            return;
        }

        LauncherView? launcher = null;
        launcher = surface.Add(new LauncherView(examples, picked => MainDispatcher.Enqueue(() =>
        {
            surface.Remove(launcher!);
            Run(picked, surface);
        })));
    }

    private void Run(Example picked, IWindowSurface surface)
    {
        _running = picked;
        picked.Start(new ExampleContext(this, surface, args));
    }
}
