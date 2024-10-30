using rin.Editor.Modules;
using rin.Scene.Graphics;

namespace rin.Editor.Assets;

public class StaticMeshAsset : Asset
{
    public uint[] Indices = [];
    public MaterialAsset?[] Materials = [];

    public MeshSurface[] Surfaces = [];
    public StaticMesh.Vertex[] Vertices = [];

    public StaticMeshAsset(MeshSurface[] surfaces)
    {
        Surfaces = surfaces;
        Materials = surfaces.Select(e => (MaterialAsset?)null).ToArray();
    }

    public override AssetFactory GetFactory()
    {
        return AssetsModule.Get().FactoryFor<StaticMeshAsset>();
    }

    public override string GetDisplayName()
    {
        return "Static Mesh";
    }
}