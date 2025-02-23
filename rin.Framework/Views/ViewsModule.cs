using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using rin.Framework.Core;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Graphics.Windows;
using rin.Framework.Views.Font;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views;

public class ViewsModule : IViewsModule
{
    
    private readonly Dictionary<Type, IBatcher> _batchRenderers = [];
    private readonly IFontManager _fontManager = new DefaultFontManager();
    private readonly Dictionary<IWindowRenderer, WindowSurface> _windowSurfaces = new();
    private IGraphicsModule? _graphicsModule;
    private IGraphicsShader? _stencilShader;
    
    private App? _app = null;
    
    public App GetApp() => _app ?? throw new NullReferenceException();

    [PublicAPI]
    public static string ShadersDirectory { get; } = string.Empty;
    
    public event Action<WindowSurface>? OnSurfaceCreated;
    
    public event Action<WindowSurface>? OnSurfaceDestroyed;
    public IFontManager FontManager { get; set; } = new DefaultFontManager();

    public void Start(App app)
    {
        _graphicsModule = app.GraphicsModule;
        _stencilShader = _graphicsModule.GraphicsShaderFromPath(Path.Join(ShadersDirectory, "stencil_batch.slang"));
        _graphicsModule.OnRendererCreated += OnRendererCreated;
        _graphicsModule.OnRendererDestroyed += OnRendererDestroyed;
        foreach (var renderer in _graphicsModule.GetWindowRenderers()) OnRendererCreated(renderer);
    }

    //[MemberNotNull(nameof(_graphicsModule))]
    public void Stop(App app)
    {
        _fontManager.LoadSystemFonts();
        if (_graphicsModule == null) return;
        _graphicsModule.OnRendererCreated += OnRendererCreated;
        _graphicsModule.OnRendererDestroyed += OnRendererDestroyed;
        foreach (var renderer in _graphicsModule.GetWindowRenderers()) OnRendererCreated(renderer);
    }

    public void Update(float deltaSeconds)
    {
        foreach (var surface in _windowSurfaces.Values)
        {
            surface.Update(deltaSeconds);
        }
    }
    
    public void AddFont(FileUri fontPath)
    {
        FontManager.LoadFont(GetApp().FileSystem.OpenRead(fontPath));
    }

    public IBatcher GetBatchRenderer<T>() where T : IBatcher
    {
        var type = typeof(T);
        if (_batchRenderers.TryGetValue(type, out var value)) return value;
        value = Activator.CreateInstance<T>();
        _batchRenderers.Add(type, value);

        return value;
    }

    public WindowSurface? GetWindowSurface(IWindowRenderer renderer)
    {
        _windowSurfaces.TryGetValue(renderer, out var result);
        return result;
    }

    
    public WindowSurface? GetWindowSurface(IWindow window)
    {
        var renderer = _graphicsModule?.GetWindowRenderer(window);
        return renderer == null ? null : GetWindowSurface(renderer);
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
}