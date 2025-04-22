using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views.Font;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Sdf;

namespace Rin.Engine.Views;

[Module(typeof(SGraphicsModule))]
public class SViewsModule : IModule, ISingletonGetter<SViewsModule>, IUpdatable
{
    public static readonly string
        ShadersDirectory = Path.Join(SGraphicsModule.ShadersDirectory, "views");

    private readonly Dictionary<Type, IBatcher> _batchRenderers = [];
    private readonly IFontManager _fontManager = new DefaultFontManager(new TestExternalFontCache());
    private readonly Dictionary<string, Task<MtsdfFont?>> _mtsdfTasks = new();
    private readonly Dictionary<IWindowRenderer, WindowSurface> _windowSurfaces = new();
    private SGraphicsModule? _graphicsSubsystem;
    private IGraphicsShader? _stencilShader;


    public void Start(SEngine engine)
    {
        engine.OnUpdate += Update;
        _fontManager.LoadFont(SEngine.Get().Sources.Read("Engine/Fonts/NotoSans-Regular.ttf"));
        _graphicsSubsystem = engine.GetModule<SGraphicsModule>();
        if (_graphicsSubsystem == null) return;
        _graphicsSubsystem.OnRendererCreated += OnRendererCreated;
        _graphicsSubsystem.OnRendererDestroyed += OnRendererDestroyed;
        foreach (var renderer in _graphicsSubsystem.GetWindowRenderers()) OnRendererCreated(renderer);
    }

    public void Stop(SEngine engine)
    {
        engine.OnUpdate -= Update;
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
        return SEngine.Get().GetModule<SViewsModule>();
    }

    public void Update(float deltaTime)
    {
        foreach (var surface in _windowSurfaces.Values) surface.Update(deltaTime);
    }

    public event Action<WindowSurface>? OnSurfaceCreated;
    public event Action<WindowSurface>? OnSurfaceDestroyed;

    public IFontManager GetFontManager()
    {
        return _fontManager;
    }

    public void AddFont(string fontPath)
    {
        _fontManager.LoadFont(SEngine.Get().Sources.Read(fontPath));
    }

    public IBatcher GetBatcher<T>() where T : IBatcher
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
                .MakeGraphics("Engine/Shaders/Views/stencil_batch.slang");
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

    private class TestExternalFontCache : IExternalFontCache
    {
        public TestExternalFontCache()
        {
            Directory.CreateDirectory(Path.Join(SEngine.Directory, ".gen", "mtsdf"));
        }

        public Stream? Get(int id)
        {
            return null;
            var filePath = Path.Join(SEngine.Directory, ".gen", "mtsdf", $"{id}.mtsdf");
            if (!File.Exists(filePath)) return null;
            return File.OpenRead(filePath);
        }

        public void Set(int id, Stream data)
        {
            // var filePath = Path.Join(SEngine.Directory,".gen","mtsdf", $"{id}.mtsdf");
            // Task.Run(() =>
            // {
            //     if (File.Exists(filePath)) File.Delete(filePath);
            //     
            //     var stream = File.Create(filePath);
            //     data.CopyTo(stream);
            // });
        }

        public bool SupportsSet => true;
    }
}