using System.Reflection;
using aerox.Runtime.Extensions;
using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;
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
    private readonly Mutex _msdfFontMutex = new();
    private readonly Dictionary<string, MtsdfFont> _msdfFonts = new();
    private readonly Dictionary<string, Task<MtsdfFont?>> _msdfTasks = new();
    private readonly Dictionary<WindowRenderer, WidgetWindowSurface> _windowSurfaces = new();
    private SGraphicsModule? _graphicsSubsystem;

    public static SWidgetsModule Get()
    {
        return SRuntime.Get().GetModule<SWidgetsModule>();
    }

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

    public WidgetSurface? GetMainRoot()
    {
        var win = _graphicsSubsystem?.GetMainWindow();
        return win == null ? null : GetWindowSurface(win);
    }

    public override void Shutdown(SRuntime runtime)
    {
        base.Shutdown(runtime);
        _graphicsSubsystem?.WaitDeviceIdle();
        foreach (var kv in _msdfFonts) kv.Value.Dispose();
        _msdfFonts.Clear();

        foreach (var root in _windowSurfaces) root.Value.Dispose();
        _windowSurfaces.Clear();

        _graphicsSubsystem = GetEngine()?.GetModule<SGraphicsModule>();
        if (_graphicsSubsystem != null)
        {
            _graphicsSubsystem.OnRendererCreated -= OnRendererCreated;
            _graphicsSubsystem.OnRendererDestroyed -= OnRendererDestroyed;
        }
    }

    public static MaterialInstance CreateMaterial(Shader shader)
    {
        return new MaterialBuilder().ConfigureForWidgets().SetShader(shader)
            .AddAttachmentFormats(EImageFormat.Rgba32SFloat).Build();
    }

    public static MaterialInstance CreateMaterial(string shader)
    {
        var graphicsModule = SRuntime.Get().GetModule<SGraphicsModule>();
        return new MaterialBuilder().ConfigureForWidgets().SetShader(graphicsModule.LoadShader(shader))
            .AddAttachmentFormats(EImageFormat.Rgba32SFloat).Build();
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
        }, EImageFormat.Rgba8UNorm, usage, mipMap);
    }

    private async Task<MtsdfFont?> GenerateMsdfFont(FontFamily family)
    {
        var newFont = new MtsdfGenerator(family);
        _msdfFonts.Add(family.Name, await newFont.GenerateFont(32));
        return _msdfFonts[family.Name];
    }

    public Task<MtsdfFont?> GetOrCreateFont(string fontFamily)
    {
        lock (_msdfFontMutex)
        {
            var family = FindFontFamily(fontFamily);

            if (family == null) return Task.FromResult<MtsdfFont?>(null);

            if (_msdfFonts.TryGetValue(family.Value.Name, out var font)) return Task.FromResult<MtsdfFont?>(font);

            if (_msdfTasks.TryGetValue(family.Value.Name, out var msdfFontTask)) return msdfFontTask;

            var task = GenerateMsdfFont(family.Value);

            task.Then(_ => _msdfTasks.Remove(family.Value.Name));

            _msdfTasks.Add(family.Value.Name, task);

            return _msdfTasks[family.Value.Name];
        }
    }
}