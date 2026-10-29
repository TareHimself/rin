using Rin.Core.Graphics;
using Rin.Core.Views.Window;

namespace Examples;

public abstract class Example
{
    public abstract string Name { get; }

    public abstract string Title { get; }

    public virtual Extent2D WindowSize => new(1280, 800);

    public abstract void Start(ExampleContext context);

    /// <summary>
    ///     A window the example opened has its surface. Views added to it die with the window.
    /// </summary>
    public virtual void OnChildSurface(IWindowSurface surface)
    {
    }

    public virtual void Stop()
    {
    }
}
