﻿using System.Reflection;
using rin.Framework.Core;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Graphics.Windows;
using rin.Framework.Core.Extensions;
using rin.Framework.Views.Font;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Sdf;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Views;

[Module( typeof(SGraphicsModule))]
public class SViewsModule : IModule, ISingletonGetter<SViewsModule>
{
    private readonly IFontManager _fontManager = new DefaultFontManager();
    private readonly Mutex _mtsdfFontMutex = new();
    private readonly Dictionary<string, MtsdfFont> _mtsdfFonts = new();
    private readonly Dictionary<string, Task<MtsdfFont?>> _mtsdfTasks = new();
    private readonly Dictionary<WindowRenderer, WindowSurface> _windowSurfaces = new();
    private readonly Dictionary<Type, IBatcher> _batchRenderers = [];
    private SGraphicsModule? _graphicsSubsystem;
    private IGraphicsShader? _stencilShader = null;
    
    public event Action<WindowSurface>? OnSurfaceCreated;
    public event Action<WindowSurface>? OnSurfaceDestroyed;
    
    public static readonly string
        ShadersDirectory = Path.Join(SGraphicsModule.ShadersDirectory,"views");
    
    public static SViewsModule Get() => SRuntime.Get().GetModule<SViewsModule>();

    public IFontManager GetFontManager()
    {
        return _fontManager;
    }
    public void AddFont(string fontPath,IFileSystem? fileSystem = null)
    {
        var fs = fileSystem ?? SRuntime.Get().FileSystem;
        _fontManager.LoadFont(fs.OpenRead(fontPath));
    }

    public IBatcher GetBatchRenderer<T>() where T : IBatcher
    {
        var type = typeof(T);
        if (_batchRenderers.TryGetValue(type, out IBatcher? value)) return value;
        value = Activator.CreateInstance<T>();
        _batchRenderers.Add(type, value);
        
        return value;
    }
    

    public void Startup(SRuntime runtime)
    {
        _fontManager.LoadSystemFonts();
        _graphicsSubsystem = runtime.GetModule<SGraphicsModule>();
        if (_graphicsSubsystem == null) return;
        _graphicsSubsystem.OnRendererCreated += OnRendererCreated;
        _graphicsSubsystem.OnRendererDestroyed += OnRendererDestroyed;
        foreach (var renderer in _graphicsSubsystem.GetRenderers()) OnRendererCreated(renderer);
    }

    private void OnRendererCreated(WindowRenderer renderer)
    {
        var root = new WindowSurface(renderer);
        _windowSurfaces.Add(renderer, root);
        root.Init();
        if (_stencilShader == null)
        {
            _stencilShader = SGraphicsModule.Get().GraphicsShaderFromPath(Path.Join(ShadersDirectory,"stencil_batch.slang"));
        }
        OnSurfaceCreated?.Invoke(root);
    }

    private void OnRendererDestroyed(WindowRenderer renderer)
    {
        OnSurfaceDestroyed?.Invoke(_windowSurfaces[renderer]);
        _windowSurfaces[renderer].Dispose();
        _windowSurfaces.Remove(renderer);
    }

    public WindowSurface? GetWindowSurface(WindowRenderer renderer)
    {
        _windowSurfaces.TryGetValue(renderer, out var result);

        return result;
    }

    public WindowSurface? GetWindowSurface(IWindow window)
    {
        var renderer = _graphicsSubsystem?.GetWindowRenderer(window);
        return renderer == null ? null : GetWindowSurface(renderer);
    }

    public void Shutdown(SRuntime runtime)
    {
        _graphicsSubsystem?.WaitDeviceIdle();
        foreach (var kv in _mtsdfFonts) kv.Value.Dispose();
        _mtsdfFonts.Clear();

        foreach (var (_,surf) in _windowSurfaces)
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
    public IGraphicsShader? GetStencilShader() => _stencilShader;
}