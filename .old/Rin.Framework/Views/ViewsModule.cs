using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Font;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Window;

namespace Rin.Framework.Views;

public class ViewsModule : IViewsModule
{
    private readonly Dictionary<Type, IBatcher> _batchRenderers = [];
    private readonly Dictionary<IWindowRenderer, WindowSurface> _windowSurfaces = new();
    private IGraphicsModule? _graphicsSubsystem;
    public void Start(IApplication app)
    {
        app.OnUpdate += Update;
        FontManager.LoadFont(SFramework.Sources.Read("Framework/Fonts/NotoSans-Regular.ttf"));
        _graphicsSubsystem = IGraphicsModule.Get();
        if (_graphicsSubsystem == null) return;
        _graphicsSubsystem.OnWindowRendererCreated += OnRendererCreated;
        _graphicsSubsystem.OnWindowRendererDestroyed += OnRendererDestroyed;
        foreach (var renderer in _graphicsSubsystem.GetWindowRenderers()) OnRendererCreated(renderer);
    }

    public void Stop(IApplication app)
    {
        app.OnUpdate -= Update;
        _graphicsSubsystem?.WaitIdle();

        foreach (var (_, surf) in _windowSurfaces)
        {
            OnSurfaceDestroyed?.Invoke(surf);
            surf.Dispose();
        }

        _windowSurfaces.Clear();

        _graphicsSubsystem = IGraphicsModule.Get();
        if (_graphicsSubsystem != null)
        {
            _graphicsSubsystem.OnWindowRendererCreated -= OnRendererCreated;
            _graphicsSubsystem.OnWindowRendererDestroyed -= OnRendererDestroyed;
        }
    }

    public void Update(float deltaTime)
    {
        foreach (var surface in _windowSurfaces.Values) surface.Update(deltaTime);
    }

    public event Action<IWindowSurface>? OnSurfaceCreated;
    public event Action<IWindowSurface>? OnSurfaceDestroyed;
    public IFontManager FontManager { get; } = new SixLaborsFontManager();
    public void AddFont(string fontPath)
    {
        FontManager.LoadFont(SFramework.Sources.Read(fontPath));
    }

    public IBatcher GetBatcher<T>() where T : IBatcher
    {
        var type = typeof(T);
        if (_batchRenderers.TryGetValue(type, out var value)) return value;
        value = Activator.CreateInstance<T>();
        _batchRenderers.Add(type, value);

        return value;
    }

    public IWindowSurface? GetWindowSurface(IWindowRenderer renderer)
    {
        _windowSurfaces.TryGetValue(renderer, out var result);

        return result;
    }

    public IWindowSurface? GetWindowSurface(IWindow window)
    {
        var renderer = _graphicsSubsystem?.GetWindowRenderer(window);
        return renderer == null ? null : GetWindowSurface(renderer);
    }
    
    private void OnRendererCreated(IWindowRenderer renderer)
    {
        var root = new WindowSurface(renderer);
        _windowSurfaces.Add(renderer, root);
        root.Init();
        OnSurfaceCreated?.Invoke(root);
    }

    private void OnRendererDestroyed(IWindowRenderer renderer)
    {
        OnSurfaceDestroyed?.Invoke(_windowSurfaces[renderer]);
        _windowSurfaces[renderer].Dispose();
        _windowSurfaces.Remove(renderer);
    }
}