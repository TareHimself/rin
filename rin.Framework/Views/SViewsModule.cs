using rin.Framework.Core;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Graphics.Windows;
using rin.Framework.Views.Font;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Sdf;

namespace rin.Framework.Views;

[Module(typeof(SGraphicsModule))]
public class SViewsModule : IModule, ISingletonGetter<SViewsModule>
{
    public static readonly string
        ShadersDirectory = Path.Join(SGraphicsModule.ShadersDirectory, "views");

    private readonly Dictionary<Type, IBatcher> _batchRenderers = [];
    private readonly IFontManager _fontManager = new DefaultFontManager();
    private readonly Dictionary<string, Task<MtsdfFont?>> _mtsdfTasks = new();
    private readonly Dictionary<IWindowRenderer, WindowSurface> _windowSurfaces = new();
    private SGraphicsModule? _graphicsSubsystem;
    private IGraphicsShader? _stencilShader;


    public void Start(SRuntime runtime)
    {
        _fontManager.LoadSystemFonts();
        _graphicsSubsystem = runtime.GetModule<SGraphicsModule>();
        if (_graphicsSubsystem == null) return;
        _graphicsSubsystem.OnRendererCreated += OnRendererCreated;
        _graphicsSubsystem.OnRendererDestroyed += OnRendererDestroyed;
        foreach (var renderer in _graphicsSubsystem.GetWindowRenderers()) OnRendererCreated(renderer);
    }

    public void Stop(SRuntime runtime)
    {
        _graphicsSubsystem?.WaitDeviceIdle();

        foreach (var (_, surf) in _windowSurfaces)
        {
            OnSurfaceDestroyed?.Invoke(surf);
            surf.Dispose();
        }

        _windowSurfaces.Clear();

        _graphicsSubsystem = SGraphicsModule.Get();
        if (_graphicsSubsystem != null)
        {
            _graphicsSubsystem.OnRendererCreated -= OnRendererCreated;
            _graphicsSubsystem.OnRendererDestroyed -= OnRendererDestroyed;
        }
    }

    public static SViewsModule Get()
    {
        return SRuntime.Get().GetModule<SViewsModule>();
    }

    public event Action<WindowSurface>? OnSurfaceCreated;
    public event Action<WindowSurface>? OnSurfaceDestroyed;

    public IFontManager GetFontManager()
    {
        return _fontManager;
    }

    public void AddFont(string fontPath, IFileSystem? fileSystem = null)
    {
        var fs = fileSystem ?? SRuntime.Get().FileSystem;
        _fontManager.LoadFont(fs.OpenRead(FileUri.FromSystemPath(fontPath)));
    }

    public IBatcher GetBatchRenderer<T>() where T : IBatcher
    {
        var type = typeof(T);
        if (_batchRenderers.TryGetValue(type, out var value)) return value;
        value = Activator.CreateInstance<T>();
        _batchRenderers.Add(type, value);

        return value;
    }

    private void OnRendererCreated(IWindowRenderer renderer)
    {
        var root = new WindowSurface(renderer);
        _windowSurfaces.Add(renderer, root);
        root.Init();
        if (_stencilShader == null)
            _stencilShader = SGraphicsModule.Get()
                .GraphicsShaderFromPath(Path.Join(ShadersDirectory, "stencil_batch.slang"));
        OnSurfaceCreated?.Invoke(root);
    }

    private void OnRendererDestroyed(IWindowRenderer renderer)
    {
        OnSurfaceDestroyed?.Invoke(_windowSurfaces[renderer]);
        _windowSurfaces[renderer].Dispose();
        _windowSurfaces.Remove(renderer);
    }

    public WindowSurface? GetWindowSurface(IWindowRenderer renderer)
    {
        _windowSurfaces.TryGetValue(renderer, out var result);

        return result;
    }

    public WindowSurface? GetWindowSurface(IWindow window)
    {
        var renderer = _graphicsSubsystem?.GetWindowRenderer(window);
        return renderer == null ? null : GetWindowSurface(renderer);
    }

    public IGraphicsShader? GetStencilShader()
    {
        return _stencilShader;
    }

    public void Update(double deltaTime)
    {
        foreach (var surface in _windowSurfaces.Values) surface.Update(deltaTime);
    }
}