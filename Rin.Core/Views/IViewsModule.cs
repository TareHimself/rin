using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Font;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Window;

namespace Rin.Core.Views;

public interface IViewsModule : IModule, IUpdatable
{
    public IFontManager FontManager { get; }
    public event Action<IWindowSurface>? OnSurfaceCreated;
    public event Action<IWindowSurface>? OnSurfaceDestroyed;

    public void AddFont(string fontPath);

    public IBatcher GetBatcher<T>() where T : IBatcher, new();

    public IWindowSurface? GetWindowSurface(IWindowRenderer renderer);

    public IWindowSurface? GetWindowSurface(IWindow window);

    public static IViewsModule Get()
    {
        return Global.Provider.Get<IViewsModule>();
    }
}