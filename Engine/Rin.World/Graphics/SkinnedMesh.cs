using Rin.World.Animation;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics;

/// <summary>
///     Draws the <see cref="IMeshFactory" /> mesh <see cref="Mesh" /> posed by <see cref="PoseSource" />, which also
///     supplies the skeleton. The proxy is created when the component is added, so set every field first.
/// </summary>
public struct SkinnedMesh : IComponentData
{
    public MeshHandle Mesh;
    public MaterialSet Materials;
    public PoseSourceHandle PoseSource;
}
