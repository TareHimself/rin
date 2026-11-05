
namespace Rin.World.Graphics;

/// <summary>
///     Added and removed by <see cref="RenderFeature" /> alongside a mesh or light component, and rebuilt by it after a
///     snapshot restore.
/// </summary>
public struct RenderProxy : IDerivedComponentData
{
    public RenderProxyHandle Handle { get; internal set; }

    /// <summary>
    ///     The <see cref="Transforms.WorldTransform.Version" /> last pushed to the render system.
    /// </summary>
    public uint LastPushedVersion { get; internal set; }
}
