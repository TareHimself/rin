using System.Numerics;
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
    public void LoadStaticMeshMakesOneSurfacePerPrimitive()
    {
        var mesh = GltfMeshImporter.LoadStaticMesh(CubePath);

        Assert.That(mesh, Is.Not.Null);
        var surfaces = IMeshFactory.Get().GetMesh(mesh!.Value)!.GetSurfaces();
        Assert.That(surfaces.Select(s => (s.VertexStart, s.VertexCount, s.IndicesStart, s.IndicesCount)),
            Is.EqualTo(new[] { (0u, 144u, 0u, 288u), (144u, 24u, 288u, 36u) }), "one surface per primitive, packed back to back");
        Assert.That(Vector3.Distance(surfaces[1].Bounds.Max, new Vector3(0.85f)), Is.LessThan(1e-4f));
    }

    [Test]
    public void LoadSkinnedMeshLoadsMeshAndSkeleton()
    {
        var mesh = GltfMeshImporter.LoadSkinnedMesh(FoxPath);

        Assert.That(mesh, Is.Not.Null);
        Assert.That(mesh!.Skeleton.Bones, Has.Length.EqualTo(24));
        Assert.That(mesh.Skeleton.BoneNameToIndex, Contains.Key("b_Hip_01"));
    }

    [Test]
    public void APrimitiveWithoutIndicesDrawsItsVerticesInOrder()
    {
        Assert.That(GltfMeshImporter.SequentialIndices(4), Is.EqualTo(new uint[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void MissingNormalsAreAveragedFromTheFacesAroundEachVertex()
    {
        Vector3[] positions = [Vector3.Zero, Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY];
        uint[] indices = [0, 2, 1, 0, 1, 3];

        var normals = GltfMeshImporter.ComputeNormals(positions, indices);

        Assert.That(normals[2], Is.EqualTo(Vector3.UnitY), "only the floor triangle touches it");
        Assert.That(normals[3], Is.EqualTo(Vector3.UnitZ), "only the wall triangle touches it");
        Assert.That(Vector3.Distance(normals[0], Vector3.Normalize(Vector3.UnitY + Vector3.UnitZ)), Is.LessThan(1e-5f));
    }

    [Test]
    public void AVertexNoTriangleUsesGetsAnUpNormal()
    {
        Vector3[] positions = [Vector3.Zero, Vector3.UnitX, Vector3.UnitZ, new(5f, 5f, 5f)];
        uint[] indices = [0, 1, 2];

        Assert.That(GltfMeshImporter.ComputeNormals(positions, indices)[3], Is.EqualTo(Vector3.UnitY));
    }
}
