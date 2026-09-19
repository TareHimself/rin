using Rin.Core;
using Rin.Core.Graphics;
using Rin.Graphics.Null;

namespace Rin.GLTF.Tests;

public class GltfMeshImporterTests
{
    private static string CubePath => Path.Combine(AppContext.BaseDirectory, "Assets", "cube.glb");
    private static string FoxPath => Path.Combine(AppContext.BaseDirectory, "Assets", "fox.glb");

    [OneTimeSetUp]
    public void RegisterNullGraphicsModule()
    {
        Global.Provider.AddSingle<IGraphicsModule>(new NullGraphicsModule());
    }

    [Test]
    public async Task LoadStaticMeshLoadsARealMesh()
    {
        var mesh = await GltfMeshImporter.LoadStaticMesh(CubePath);

        Assert.That(mesh, Is.Not.Null);
    }

    [Test]
    public async Task LoadSkinnedMeshLoadsMeshAndSkeleton()
    {
        var mesh = await GltfMeshImporter.LoadSkinnedMesh(FoxPath);

        Assert.That(mesh, Is.Not.Null);
        Assert.That(mesh!.Skeleton.Bones, Has.Length.EqualTo(24));
        Assert.That(mesh.Skeleton.BoneNameToIndex, Contains.Key("b_Hip_01"));
    }
}
