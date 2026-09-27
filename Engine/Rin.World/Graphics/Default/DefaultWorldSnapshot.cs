using JetBrains.Annotations;
using Rin.Core.Graphics.Graph;

namespace Rin.World.Graphics.Default;

/// <summary>
///     View-independent snapshot of a world's render proxies, shared by every view of that world until the
///     proxies change. Each graph turns it into GPU work once through <see cref="Write" />.
/// </summary>
public sealed class DefaultWorldSnapshot(
    StaticMeshInfo[] staticGeometry,
    SkinnedMeshInfo[] skinnedGeometry,
    LightInfo[] lights)
{
    [PublicAPI] public StaticMeshInfo[] StaticGeometry { get; } = staticGeometry;
    [PublicAPI] public SkinnedMeshInfo[] SkinnedGeometry { get; } = skinnedGeometry;
    public LightInfo[] Lights { get; } = lights;

    public DefaultSceneFrame Write(IGraphBuilder builder)
    {
        return builder.GetOrAddShared(this, b => new DefaultSceneFrame(this, b));
    }
}
