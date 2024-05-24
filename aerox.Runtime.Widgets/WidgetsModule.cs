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

[NativeRuntimeModule(typeof(WindowsModule), typeof(GraphicsModule))]
public class WidgetsModule : RuntimeModule,ISingletonGetter<WidgetsModule>
{
    private readonly FontCollection _fontCollection = new();
    private readonly Mutex _msdfFontMutex = new();
    private readonly Dictionary<string, MsdfFont> _msdfFonts = new();
    private readonly Dictionary<string, Task<MsdfFont?>> _msdfTasks = new();
    private readonly Dictionary<Graphics.WindowRenderer, WidgetWindowSurface> _windowSurfaces = new();
    private GraphicsModule? _graphicsSubsystem;

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

    public override async void Startup(Runtime runtime)
    {
        base.Startup(runtime);
        _fontCollection.AddSystemFonts();
        _graphicsSubsystem = runtime.GetModule<GraphicsModule>();
        if (_graphicsSubsystem == null) return;
        _graphicsSubsystem.OnRendererCreated += OnRendererCreated;
        _graphicsSubsystem.OnRendererDestroyed += OnRendererDestroyed;
        foreach (var renderer in _graphicsSubsystem.GetRenderers()) OnRendererCreated(renderer);
    }

    private void OnRendererCreated(Graphics.WindowRenderer renderer)
    {
        var root = new WidgetWindowSurface(renderer);
        _windowSurfaces.Add(renderer,root );
        root.Init();
    }

    private void OnRendererDestroyed(Graphics.WindowRenderer renderer)
    {
        _windowSurfaces[renderer].Dispose();
        _windowSurfaces.Remove(renderer);
    }

    public WidgetWindowSurface? GetWindowSurface(Graphics.WindowRenderer renderer)
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
    /// Returns the <see cref="WidgetWindowSurface"/> of the main window
    /// </summary>
    /// <returns></returns>
    public WidgetWindowSurface? GetWindowSurface()
    {
        var mainWindow = _graphicsSubsystem?.GetMainWindow();
        return mainWindow == null ?  null : GetWindowSurface(mainWindow);
    }

    public WidgetSurface? GetMainRoot()
    {
        var win = _graphicsSubsystem?.GetMainWindow();
        return win == null ? null : GetWindowSurface(win);
    }

    public override void Shutdown(Runtime runtime)
    {
        base.Shutdown(runtime);
        _graphicsSubsystem?.WaitDeviceIdle();
        foreach (var kv in _msdfFonts) kv.Value.Dispose();
        _msdfFonts.Clear();

        foreach (var root in _windowSurfaces) root.Value.Dispose();
        _windowSurfaces.Clear();

        _graphicsSubsystem = GetEngine()?.GetModule<GraphicsModule>();
        if (_graphicsSubsystem != null)
        {
            _graphicsSubsystem.OnRendererCreated -= OnRendererCreated;
            _graphicsSubsystem.OnRendererDestroyed -= OnRendererDestroyed;
        }
    }

    public static MaterialInstance CreateMaterial(params Shader[] shaders)
    {
        return new MaterialBuilder().ConfigureForWidgets().AddShaders(shaders)
            .AddAttachmentFormats(VkFormat.VK_FORMAT_R16G16B16A16_SFLOAT).Build();
    }

    public static MaterialInstance CreateMaterial(params string[] shaders)
    {
        var graphicsModule = Runtime.Instance.GetModule<GraphicsModule>();
        return new MaterialBuilder().ConfigureForWidgets().AddShaders(shaders.Select(graphicsModule.LoadShader))
            .AddAttachmentFormats(VkFormat.VK_FORMAT_R16G16B16A16_SFLOAT).Build();
    }

    public static DeviceImage? UploadImage(Image<Rgba32> image, VkImageUsageFlags usage, bool mipMap = false)
    {
        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        var data = ms.ToArray();
        return Runtime.Instance.GetModule<GraphicsModule>()?.CreateImage(data, new VkExtent3D
        {
            width = (uint)image.Width,
            height = (uint)image.Height,
            depth = 1
        }, ImageFormat.Rgba8, usage, mipMap);
    }

    private async Task<MsdfFont?> GenerateMsdfFont(FontFamily family)
    {
        var newFont = new MsdfGenerator(family);
        _msdfFonts.Add(family.Name, await newFont.GenerateFont(32));
        return _msdfFonts[family.Name];
    }

    public Task<MsdfFont?> GetOrCreateFont(string fontFamily)
    {
        lock (_msdfFontMutex)
        {
            var family = Runtime.Instance.GetModule<WidgetsModule>().FindFontFamily(fontFamily);

            if (family == null) return Task.FromResult<MsdfFont?>(null);

            if (_msdfFonts.TryGetValue(family.Value.Name, out var font)) return Task.FromResult<MsdfFont?>(font);

            if (_msdfTasks.TryGetValue(family.Value.Name, out var msdfFontTask)) return msdfFontTask;

            var task = GenerateMsdfFont(family.Value);

            task.Then(_ => _msdfTasks.Remove(family.Value.Name));

            _msdfTasks.Add(family.Value.Name, task);

            return _msdfTasks[family.Value.Name];
        }
    }

    public static WidgetsModule Get()
    {
        return Runtime.Instance.GetModule<WidgetsModule>();
    }
}