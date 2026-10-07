using Rin.Core.Graphics;

namespace Examples;

public abstract class Example
{
    public abstract string Name { get; }

    public abstract string Title { get; }

    public virtual Extent2D WindowSize => new(1280, 800);

    public abstract void Start(ExampleContext context);

    public virtual void Stop()
    {
    }
}
