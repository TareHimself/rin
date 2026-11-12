using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics;

/// <summary>
///     Draws the <see cref="IMeshFactory" /> mesh <see cref="Mesh" /> with one material per surface from
///     <see cref="Materials" />. The proxy is created when the component is added, so set both fields first.
/// </summary>
public struct StaticMesh : IComponentData
{
    public MeshHandle Mesh;
    public MaterialSet Materials;
}
