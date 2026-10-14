using Examples.Common;
using Rin.Core;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Sources;
using Rin.Core.Views;
using Rin.Core.Views.Window;

namespace Examples;

public sealed class ExampleHost(Example? example, IReadOnlyList<Example> examples, string[] args) : ExampleApplication
{
    private const string TextFontAsset = "fonts/LINESeedJP-Regular.ttf";
    private const string TextFontFamily = "LINE Seed JP";
    private static readonly Extent2D LauncherSize = new(1280, 800);
    private static readonly Log Log = Log.For<ExampleHost>();

    private IWindowSurface? _surface;
    private Example? _running;
    private ExampleContext? _context;
    private LauncherView? _launcher;
    private BackButtonView? _back;
    private float _volume;
    private float _masterVolume;

    protected override void OnStartup()
    {
        Global.Sources.AddSource(AssemblyContentResource.New<ExampleHost>("Shaders/Examples"));
        LoadTextFont();

        IViewsModule.Get().OnSurfaceCreated += OnSurfaceCreated;
        IGraphicsModule.Get().OnWindowCreated += OnWindowCreated;
        IGraphicsModule.Get().CreateWindow(example?.Title ?? "Rin Examples", example?.WindowSize ?? LauncherSize,
            WindowFlags.Visible | WindowFlags.Resizable);
    }

    protected override void OnShutdown()
    {
        StopRunning();
        base.OnShutdown();
    }

    /// <summary>
    ///     A file under the <c>assets</c> folder copied next to the executable.
    /// </summary>
    public static string AssetPath(string relativePath)
    {
        return Path.Combine(AppContext.BaseDirectory, "assets", relativePath);
    }

    private static void LoadTextFont()
    {
        var views = IViewsModule.Get();
        using var stream = File.OpenRead(AssetPath(TextFontAsset));
        views.FontManager.LoadFont(stream);
        views.DefaultFontFamily = TextFontFamily;
        Log.Info($"Loaded '{TextFontFamily}' as the default font");
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
        if (_surface is not null) return;
        _surface = surface;

        if (example is not null)
            Run(example, surface);
        else
            ShowLauncher(surface);
    }

    private void ShowLauncher(IWindowSurface surface)
    {
        _launcher = surface.Add(new LauncherView(examples, picked => MainDispatcher.Enqueue(() => Open(picked, surface))));
    }

    private void Open(Example picked, IWindowSurface surface)
    {
        CloseLauncher(surface);
        Run(picked, surface);
        _back = surface.Add(new BackButtonView(() => MainDispatcher.Enqueue(() => BackToLauncher(surface))));
    }

    private void CloseLauncher(IWindowSurface surface)
    {
        if (_launcher is null) return;
        surface.Remove(_launcher);
        _launcher.Dispose();
        _launcher = null;
    }

    private void Run(Example picked, IWindowSurface surface)
    {
        var audio = IAudioModule.Get();
        _volume = audio.GetVolume();
        _masterVolume = audio.MasterAudioGroup.Volume;

        Log.Info($"Starting '{picked.Name}'");
        _running = picked;
        _context = new ExampleContext(this, surface, args);
        _context.ViewAdded += () => KeepBackButtonOnTop(surface);
        picked.Start(_context);
    }

    private void BackToLauncher(IWindowSurface surface)
    {
        if (_running is null) return;

        StopRunning();

        var audio = IAudioModule.Get();
        audio.SetVolume(_volume);
        audio.MasterAudioGroup.Volume = _masterVolume;

        if (_back is not null)
        {
            surface.Remove(_back);
            _back.Dispose();
            _back = null;
        }

        ShowLauncher(surface);
    }

    private void StopRunning()
    {
        if (_running is not null) Log.Info($"Stopping '{_running.Name}'");
        _context?.Release();
        _context = null;
        _running?.Stop();
        _running = null;
    }

    private void KeepBackButtonOnTop(IWindowSurface surface)
    {
        if (_back is null) return;
        surface.Remove(_back);
        surface.Add(_back);
    }
}
