using Rin.Core;
using Rin.Core.Graphics;
using Rin.Graphics.Null;
using Rin.World.Graphics.Mesh;

namespace Rin.GLTF.Tests;

public class GltfMeshImporterTests
{
    private static string CubePath => Path.Combine(AppContext.BaseDirectory, "Assets", "cube.glb");
    private static string FoxPath => Path.Combine(AppContext.BaseDirectory, "Assets", "fox.glb");

    [OneTimeSetUp]
    public void RegisterNullGraphicsModule()
    {
        Global.Provider.AddSingle<IGraphicsModule>(new NullGraphicsModule());
        Global.Provider.AddSingle<IMeshFactory>(new MeshFactory());
    }

    [Test]
    public void LoadStaticMeshLoadsARealMesh()
    {
        var mesh = GltfMeshImporter.LoadStaticMesh(CubePath);

        Assert.That(mesh, Is.Not.Null);
        Assert.That(IMeshFactory.Get().GetMesh(mesh!.Value), Is.Not.Null);
    }

    [Test]
    public void LoadSkinnedMeshLoadsMeshAndSkeleton()
    {
        var mesh = GltfMeshImporter.LoadSkinnedMesh(FoxPath);

        Assert.That(mesh, Is.Not.Null);
        Assert.That(mesh!.Skeleton.Bones, Has.Length.EqualTo(24));
        Assert.That(mesh.Skeleton.BoneNameToIndex, Contains.Key("b_Hip_01"));
    }
}
