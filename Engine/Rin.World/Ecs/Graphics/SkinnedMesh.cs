using Rin.World.Ecs.Animation;

namespace Rin.World.Ecs.Graphics;

/// <summary>
///     Draws the <see cref="Rin.World.Graphics.Mesh.IMeshFactory" /> mesh <see cref="MeshId" /> posed by
///     <see cref="PoseSource" />, which also supplies the skeleton. The proxy is created when the component is added, so
///     set every field first.
/// </summary>
public struct SkinnedMesh : IComponentData
{
    public int MeshId;
    public MaterialSet Materials;
    public PoseSourceHandle PoseSource;
}
