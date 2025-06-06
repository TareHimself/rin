using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.World.Components;

namespace Rin.Engine.World.Graphics;

/// <summary>
///     Stateful world renderer
/// </summary>
public interface IWorldRenderer : IDisposable
{
    /// <summary>
    ///     Will create passes to render the <see cref="World" /> from the perspective of the <see cref="view" />
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="view"></param>
    /// <param name="extent"></param>
    public IWorldRenderContext Collect(IGraphBuilder builder, CameraComponent view, in Extent2D extent);
}