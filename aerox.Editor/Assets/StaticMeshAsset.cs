using aerox.Editor.Modules;
using aerox.Runtime.Scene.Graphics;

namespace aerox.Editor.Assets;

public class StaticMeshAsset : Assets.Asset
{
    
    public MeshSurface[] Surfaces = [];
    public MaterialAsset?[] Materials = [];
    public StaticMesh.Vertex[] Vertices = [];
    public uint[] Indices = [];
    
    public StaticMeshAsset(MeshSurface[] surfaces) : base()
    {
        Surfaces = surfaces;
        Materials = surfaces.Select((e) => (MaterialAsset?)null).ToArray();
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