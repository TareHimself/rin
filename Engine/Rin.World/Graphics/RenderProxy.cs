
namespace Rin.World.Graphics;

/// <summary>
///     Added and removed by <see cref="RenderFeature" /> alongside a mesh or light component.
/// </summary>
public struct RenderProxy : IComponentData
{
    public RenderProxyHandle Handle { get; internal set; }

    /// <summary>
    ///     The <see cref="Transforms.WorldTransform.Version" /> last pushed to the render system.
    /// </summary>
    public uint LastPushedVersion { get; internal set; }
}
