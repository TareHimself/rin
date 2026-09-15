using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.World.Components;

namespace Rin.World.Graphics;

/// <summary>
///     Stateful world renderer. <see cref="Snapshot" /> reads the live <see cref="World" /> and must
///     run on the collect (main) thread; <see cref="Build" /> only touches that snapshot and runs on
///     the render thread.
/// </summary>
public interface IWorldRenderer : IDisposable
{
    /// <summary>
    ///     Walk the <see cref="World" /> the <paramref name="view" /> belongs to and capture an immutable
    ///     render snapshot. MUST be called on the collect (main) thread.
    /// </summary>
    public IWorldRenderContext Snapshot(CameraComponent view, in Extent2D extent);

    /// <summary>
    ///     Wire a snapshot produced by <see cref="Snapshot" /> into the frame graph. Runs on the render
    ///     thread and touches no live world state.
    /// </summary>
    public void Build(IGraphBuilder builder, IWorldRenderContext context);
}
