using System.Reflection;
using aerox.Runtime.Extensions;
using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Graphics.Shaders;
using aerox.Runtime.Widgets.Animation;
using aerox.Runtime.Widgets.Mtsdf;
using aerox.Runtime.Windows;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Widgets;

[NativeRuntimeModule(typeof(SWindowsModule), typeof(SGraphicsModule))]
public class SWidgetsModule : RuntimeModule, ISingletonGetter<SWidgetsModule>
{
    
    public static readonly string
        ShadersDir = Path.Join(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? "") ?? "","shaders","widgets");
    private readonly FontCollection _fontCollection = new();
    private readonly Mutex _mtsdfFontMutex = new();
    private readonly Dictionary<string, MtsdfFont> _mtsdfFonts = new();
    private readonly Dictionary<string, Task<MtsdfFont?>> _mtsdfTasks = new();
    private readonly Dictionary<WindowRenderer, WidgetWindowSurface> _windowSurfaces = new();
    private SGraphicsModule? _graphicsSubsystem;

    public readonly AnimationProcessor Processor = new AnimationProcessor();

    public string RunAnimation(Widget widget,AnimationAction animation) => Processor.Run(widget,animation);
    
    public static SWidgetsModule Get() => SRuntime.Get().GetModule<SWidgetsModule>();

    public FontCollection GetFontCollection()
    {
        return _fontCollection;
    }

    public void AddFont(string fontPath)
    {
        _fontCollection.Add(fontPath);
    }

    public FontFamily? FindFontFamily(string name)
    {
        if (_fontCollection.TryGet(name, out var fontFamily)) return fontFamily;

        return null;
    }

    public override async void Startup(SRuntime runtime)
    {
        base.Startup(runtime);
        _fontCollection.AddSystemFonts();
        _graphicsSubsystem = runtime.GetModule<SGraphicsModule>();
        if (_graphicsSubsystem == null) return;
        _graphicsSubsystem.OnRendererCreated += OnRendererCreated;
        _graphicsSubsystem.OnRendererDestroyed += OnRendererDestroyed;
        foreach (var renderer in _graphicsSubsystem.GetRenderers()) OnRendererCreated(renderer);
        runtime.OnTick += Tick;
    }

    public void Tick(double delta)
    {
        Processor.Apply();
    }

    private void OnRendererCreated(WindowRenderer renderer)
    {
        var root = new WidgetWindowSurface(renderer);
        _windowSurfaces.Add(renderer, root);
        root.Init();
    }

    private void OnRendererDestroyed(WindowRenderer renderer)
    {
        _windowSurfaces[renderer].Dispose();
        _windowSurfaces.Remove(renderer);
    }

    public WidgetWindowSurface? GetWindowSurface(WindowRenderer renderer)
    {
        _windowSurfaces.TryGetValue(renderer, out var result);

        return result;
    }

    public WidgetWindowSurface? GetWindowSurface(Window window)
    {
        var renderer = _graphicsSubsystem?.GetWindowRenderer(window);
        return renderer == null ? null : GetWindowSurface(renderer);
    }

    /// <summary>
    ///     Returns the <see cref="WidgetWindowSurface" /> of the main window
    /// </summary>
    /// <returns></returns>
    public WidgetWindowSurface? GetWindowSurface()
    {
        var mainWindow = _graphicsSubsystem?.GetMainWindow();
        return mainWindow == null ? null : GetWindowSurface(mainWindow);
    }

    public override void Shutdown(SRuntime runtime)
    {
        base.Shutdown(runtime);
        runtime.OnTick -= Tick;
        _graphicsSubsystem?.WaitDeviceIdle();
        foreach (var kv in _mtsdfFonts) kv.Value.Dispose();
        _mtsdfFonts.Clear();

        foreach (var root in _windowSurfaces) root.Value.Dispose();
        _windowSurfaces.Clear();

        _graphicsSubsystem = GetEngine()?.GetModule<SGraphicsModule>();
        if (_graphicsSubsystem != null)
        {
            _graphicsSubsystem.OnRendererCreated -= OnRendererCreated;
            _graphicsSubsystem.OnRendererDestroyed -= OnRendererDestroyed;
        }
    }


    public static DeviceImage UploadImage(Image<Rgba32> image, VkImageUsageFlags usage, bool mipMap = false)
    {
        using var ms = new MemoryStream();
        var data = ms.ToArray();
        return SGraphicsModule.Get().CreateImage(data, new VkExtent3D
        {
            width = (uint)image.Width,
            height = (uint)image.Height,
            depth = 1
        }, ImageFormat.Rgba8UNorm, usage, mipMap);
    }

    private async Task<MtsdfFont?> GenerateMtsdfFont(FontFamily family)
    {
        var newFont = new MtsdfGenerator(family);
        _mtsdfFonts.Add(family.Name, await newFont.GenerateFont(32));
        return _mtsdfFonts[family.Name];
    }

    public Task<MtsdfFont?> GetOrCreateFont(string fontFamily)
    {
        lock (_mtsdfFontMutex)
        {
            var family = FindFontFamily(fontFamily);

            if (family == null) return Task.FromResult<MtsdfFont?>(null);

            if (_mtsdfFonts.TryGetValue(family.Value.Name, out var font)) return Task.FromResult<MtsdfFont?>(font);

            if (_mtsdfTasks.TryGetValue(family.Value.Name, out var mtsdfFontTask)) return mtsdfFontTask;

            var task = GenerateMtsdfFont(family.Value);

            task.Then(_ => _mtsdfTasks.Remove(family.Value.Name)).ConfigureAwait(false);

            _mtsdfTasks.Add(family.Value.Name, task);

            return _mtsdfTasks[family.Value.Name];
        }
    }
}