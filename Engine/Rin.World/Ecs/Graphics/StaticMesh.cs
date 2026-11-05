namespace Rin.World.Ecs.Graphics;

/// <summary>
///     Draws the <see cref="Rin.World.Graphics.Mesh.IMeshFactory" /> mesh <see cref="MeshId" /> with one material per
///     surface from <see cref="Materials" />. The proxy is created when the component is added, so set both fields first.
/// </summary>
public struct StaticMesh : IComponentData
{
    public int MeshId;
    public MaterialSet Materials;
}
