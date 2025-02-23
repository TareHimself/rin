using rin.Framework.Core;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Graphics.Windows;
using rin.Framework.Views.Font;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views;

public interface IViewsModule : IAppModule, IUpdatable
{
    public event Action<WindowSurface>? OnSurfaceCreated;
    public event Action<WindowSurface>? OnSurfaceDestroyed;
    
    public IFontManager FontManager { get; set; }
    
    public void AddFont(FileUri fontPath);

    public IBatcher GetBatchRenderer<T>() where T : IBatcher;

    public WindowSurface? GetWindowSurface(IWindowRenderer renderer);
    

    public WindowSurface? GetWindowSurface(IWindow window);
}