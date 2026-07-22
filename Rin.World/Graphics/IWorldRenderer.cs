using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.World.Components;

namespace Rin.World.Graphics;

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