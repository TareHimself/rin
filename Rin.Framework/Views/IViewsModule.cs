using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Font;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Window;

namespace Rin.Framework.Views;

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
        return SFramework.Provider.Get<IViewsModule>();
    }
}