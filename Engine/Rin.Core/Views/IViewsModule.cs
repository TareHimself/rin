using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Font;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Window;

namespace Rin.Core.Views;

public interface IViewsModule : IModule, IUpdatable, IProviderResolvable<IViewsModule>
{
    public IFontManager FontManager { get; }

    /// <summary>
    ///     The font family a text or icon view starts with when none is set on it.
    /// </summary>
    public string DefaultFontFamily { get; set; }

    public event Action<IWindowSurface>? OnSurfaceCreated;
    public event Action<IWindowSurface>? OnSurfaceDestroyed;

    public void AddFont(string fontPath);

    public IBatcher GetBatcher<T>() where T : IBatcher, new();

    public IWindowSurface? GetWindowSurface(IWindowRenderer renderer);

    public IWindowSurface? GetWindowSurface(IWindow window);
}