using Rin.Core.Graphics.Graph;

namespace Rin.Core.Graphics;

/// <summary>
///     Contributes passes to the one graph the graphics module builds, records and submits each frame.
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>
    ///     Called on the main thread. Collects all data required by this <see cref="IRenderer " />
    /// </summary>
    /// <returns>null if this renderer will not draw</returns>
    IRenderData? Collect();

    /// <summary>
    ///     Called on the render thread with the data this renderer collected. Registers resources and adds passes to the
    ///     shared builder. Returning false means the renderer sits this frame out and <see cref="EndFrame" /> is not
    ///     called.
    /// </summary>
    bool BeginFrame(IRenderData data, IGraphBuilder builder);

    /// <summary>
    ///     Called after the graph has recorded into the frame's command buffer and before it is submitted, for
    ///     transitions the graph does not know about, such as moving a swapchain image to its present layout.
    /// </summary>
    void EndFrame(IExecutionContext ctx);
}
