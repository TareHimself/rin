using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;

namespace Rin.World.Tests.Graphics;

public class MeshBatchesTests
{
    private static readonly DeviceBufferView Indices = new(new ResourceHandle(ResourceType.Buffer, 1), 0, 64);

    private static ProcessedMesh Mesh(IMeshMaterial material)
    {
        return new ProcessedMesh
        {
            AbsoluteMeshIndex = 0,
            GeometryId = 0,
            Transform = Matrix4x4.Identity,
            SurfaceIndex = 0,
            Surface = default,
            IndexBuffer = Indices,
            IndexBufferId = 1,
            SourceVertexBufferId = 2,
            VertexBuffer = default,
            Material = material
        };
    }

    [Test]
    public void MeshesThatShareAPassAndIndexBufferBatchTogetherInMeshOrder()
    {
        var red = new Material();
        var blue = new Material();
        ProcessedMesh[] meshes = [Mesh(red), Mesh(blue), Mesh(red), Mesh(blue), Mesh(red)];

        using var batches = MeshBatches.Group(meshes, material => material.ColorPass);

        Assert.Multiple(() =>
        {
            Assert.That(batches.Count, Is.EqualTo(2));
            Assert.That(batches[0].ToArray(), Is.EqualTo(new[] { 0, 2, 4 }));
            Assert.That(batches[1].ToArray(), Is.EqualTo(new[] { 1, 3 }));
        });
    }

    [Test]
    public void TheSelectedPassDecidesTheBatches()
    {
        var sharedDepth = new Pass();
        ProcessedMesh[] meshes = [Mesh(new Material(sharedDepth)), Mesh(new Material(sharedDepth))];

        using var color = MeshBatches.Group(meshes, material => material.ColorPass);
        using var depth = MeshBatches.Group(meshes, material => material.DepthPass);

        Assert.Multiple(() =>
        {
            Assert.That(color.Count, Is.EqualTo(2));
            Assert.That(depth.Count, Is.EqualTo(1));
            Assert.That(depth[0].ToArray(), Is.EqualTo(new[] { 0, 1 }));
        });
    }

    [Test]
    public void NoMeshesMakeNoBatches()
    {
        using var batches = MeshBatches.Group([], material => material.ColorPass);

        Assert.That(batches.Count, Is.Zero);
    }

    private sealed class Material(IMaterialPass? depthPass = null) : IMeshMaterial
    {
        public IMaterialPass ColorPass { get; } = new Pass();
        public IMaterialPass DepthPass { get; } = depthPass ?? new Pass();
    }

    private sealed class Pass : IMaterialPass
    {
        public IGraphicsShader Shader { get; } = new Shader();
        public ulong GetRequiredMemory() => 0;
        public void Write(Span<byte> destination, in ProcessedMesh mesh) => throw new NotSupportedException();

        public IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer) =>
            throw new NotSupportedException();

        public void DeclareResources(IGraphConfig config, in ProcessedMesh mesh) => throw new NotSupportedException();
    }

    private sealed class Shader : IGraphicsShader
    {
        public bool Ready => true;
        public ImageFormat[] AttachmentFormats => [];
        public BlendState BlendState => BlendState.Opaque;
        public bool UsesStencil => false;
        public bool UsesDepth => true;
        public void Compile(ICompilationContext context) => throw new NotSupportedException();
        public IGraphicsBindContext? Bind(IExecutionContext ctx, bool wait = true) => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
