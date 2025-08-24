using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Font;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Window;

namespace Rin.Framework.Views;

[Module(typeof(SGraphicsModule))]
public class SViewsModule : IModule, ISingletonGetter<SViewsModule>, IUpdatable
{
    // public static readonly string
    //     ShadersDirectory = Path.Join(SGraphicsModule.ShadersDirectory, "views");

    private readonly Dictionary<Type, IBatcher> _batchRenderers = [];
    private readonly IFontManager _fontManager = SApplication.Provider.AddSingle<IFontManager>(new DefaultFontManager());
    private readonly Dictionary<IWindowRenderer, WindowSurface> _windowSurfaces = new();
    private SGraphicsModule? _graphicsSubsystem;

    public void Start(SApplication application)
    {
        application.OnUpdate += Update;
        _fontManager.LoadFont(SApplication.Get().Sources.Read("Framework/Fonts/NotoSans-Regular.ttf"));
        _graphicsSubsystem = application.GetModule<SGraphicsModule>();
        if (_graphicsSubsystem == null) return;
        _graphicsSubsystem.OnRendererCreated += OnRendererCreated;
        _graphicsSubsystem.OnRendererDestroyed += OnRendererDestroyed;
        foreach (var renderer in _graphicsSubsystem.GetWindowRenderers()) OnRendererCreated(renderer);
    }

    public void Stop(SApplication application)
    {
        application.OnUpdate -= Update;
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
        return SApplication.Get().GetModule<SViewsModule>();
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
        _fontManager.LoadFont(SApplication.Get().Sources.Read(fontPath));
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
}