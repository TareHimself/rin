using Rin.Core;
using Rin.Core.Graphics;
using Rin.Graphics.Null;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Tests.Graphics.Mesh;

public class MeshFactoryTests
{
    private static readonly Vertex[] Vertices = new Vertex[3];
    private static readonly uint[] Indices = [0, 1, 2];

    private MeshFactory _factory = null!;

    [OneTimeSetUp]
    public void RegisterNullGraphicsModule() => Global.Provider.AddSingle<IGraphicsModule>(new NullGraphicsModule());

    [OneTimeTearDown]
    public void RemoveNullGraphicsModule() => Global.Provider.RemoveSingle<IGraphicsModule>();

    [SetUp]
    public void SetUp() => _factory = new MeshFactory();

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public void ACreatedMeshIsAvailableAtOnce()
    {
        var handle = CreateTriangle();

        Assert.That(handle.IsValid, Is.True);
        Assert.That(_factory.GetMesh(handle), Is.Not.Null);
    }

    [Test]
    public void TheDefaultHandleIsNoMesh()
    {
        Assert.That(default(MeshHandle).IsValid, Is.False);
        Assert.That(_factory.GetMesh(default), Is.Null);
    }

    [Test]
    public void AFreedHandleGoesStale()
    {
        var handle = CreateTriangle();

        _factory.FreeMeshes(handle);

        Assert.That(_factory.GetMesh(handle), Is.Null);
    }

    [Test]
    public void AStaleHandleDoesNotReachTheMeshThatReusedItsSlot()
    {
        var freed = CreateTriangle();
        _factory.FreeMeshes(freed);

        var reused = CreateTriangle();

        Assert.That(reused, Is.Not.EqualTo(freed));
        Assert.That(_factory.GetMesh(freed), Is.Null);
        Assert.That(_factory.GetMesh(reused), Is.Not.Null);
    }

    [Test]
    public void FreeingAStaleHandleLeavesTheSlotsNewMeshAlone()
    {
        var freed = CreateTriangle();
        _factory.FreeMeshes(freed);
        var reused = CreateTriangle();

        _factory.FreeMeshes(freed);

        Assert.That(_factory.GetMesh(reused), Is.Not.Null);
    }

    private MeshHandle CreateTriangle() =>
        _factory.CreateMesh<Vertex>(Vertices, Indices, [new MeshSurface { VertexCount = 3, IndicesCount = 3 }]);
}
