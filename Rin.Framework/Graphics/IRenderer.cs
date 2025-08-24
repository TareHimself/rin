namespace Rin.Framework.Graphics;

public interface IRenderer : IDisposable
{
    /// <summary>
    ///     Called on the main thread. Collects all data required by this <see cref="IRenderer " />
    /// </summary>
    /// <returns>null if this renderer will not draw</returns>
    IRenderData? Collect();

    /// <summary>
    ///     Called on the main or render thread. Executes this renderer
    /// </summary>
    /// <param name="context"></param>
    void Execute(IRenderData context);
}