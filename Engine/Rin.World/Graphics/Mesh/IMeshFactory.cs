using Rin.Core;

namespace Rin.World.Graphics.Mesh;

public interface IMeshFactory : IDisposable, IProviderResolvable<IMeshFactory>
{
    /// <summary>
    ///     The mesh is drawable as soon as this returns; its uploads are queued ahead of the first frame that draws it.
    /// </summary>
    public MeshHandle CreateMesh<TVertexFormat>(ReadOnlySpan<TVertexFormat> vertices, ReadOnlySpan<uint> indices,
        MeshSurface[] surfaces) where TVertexFormat : unmanaged, IVertex;

    /// <summary>
    ///     Null for an invalid or stale handle.
    /// </summary>
    public IMesh? GetMesh(MeshHandle mesh);

    public void FreeMeshes(params ReadOnlySpan<MeshHandle> meshes);
}
