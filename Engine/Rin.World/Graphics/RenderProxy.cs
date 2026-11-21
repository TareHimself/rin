
namespace Rin.World.Graphics;

/// <summary>
///     Added and removed by <see cref="RenderFeature" /> alongside a mesh or light component, and rebuilt by it after a
///     snapshot restore.
/// </summary>
[EcsNotSaved]
public struct RenderProxy : IComponentData
{
    public RenderProxyHandle Handle { get; internal set; }
}
