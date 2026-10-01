using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

// Port of Shaders/World/Mesh/Compute/bounds_update.slang - see Fixtures/BoundsUpdateFixtureShader.cs.
// Expected output was verified to actually compile with the real Slang compiler (rin-slang compile).
public class BoundsUpdateEmitterTests
{
    [Test]
    public void EmitsBoundsUpdateFixtureShader()
    {
        var source = FixtureSource.Read("../Fixtures/BoundsUpdateFixtureShader.cs");
        var compilation = CompilationBuilder.Build(source);

        var result = ShadeEmitter.Emit(compilation);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders.Keys, Is.EquivalentTo(new[] { "BoundsUpdateFixtureShader" }));

        const string expected = """
                                 namespace Rin::Shade::Tests::Fixtures
                                 {
                                     struct Vertex
                                     {
                                         float4 locationU;
                                         float4 normalV;
                                         float4 tangent;
                                     }

                                     struct SkinnedMesh
                                     {
                                         int index;
                                         Rin::Shade::Tests::Fixtures::Vertex* vertices;
                                         uint count;
                                     }

                                     struct UpdatableBounds3D
                                     {
                                         float3 lower;
                                         float3 upper;
                                     }

                                     struct BoundsUpdatePushConstants
                                     {
                                         Rin::Shade::Tests::Fixtures::SkinnedMesh* skinnedMeshes;
                                         int totalInvocations;
                                         Rin::Shade::Tests::Fixtures::UpdatableBounds3D* output;
                                     }

                                     struct ComputeIn
                                     {
                                         uint threadId : SV_DispatchThreadID;
                                     }

                                 }

                                 extension Rin::Shade::Tests::Fixtures::UpdatableBounds3D
                                 {
                                     __init(float3 location)
                                     {
                                         this.lower = location;
                                         this.upper = location;
                                     }
                                 }

                                 extension Rin::Shade::Tests::Fixtures::Vertex
                                 {
                                     float3 getLocation()
                                     {
                                         return this.locationU.xyz;
                                     }
                                 }

                                 extension Rin::Shade::Tests::Fixtures::UpdatableBounds3D
                                 {
                                     [mutating]
                                     void update(float3 location)
                                     {
                                         this.lower = min(this.lower, location);
                                         this.upper = max(this.upper, location);
                                     }
                                 }

                                 [[vk::push_constant]] uniform ConstantBuffer<Rin::Shade::Tests::Fixtures::BoundsUpdatePushConstants, ScalarDataLayout> push;

                                 [shader("compute")]
                                 [numthreads(64, 1, 1)]
                                 void compute(Rin::Shade::Tests::Fixtures::ComputeIn input)
                                 {
                                     var index = input.threadId;
                                     if (index >= (uint)push.totalInvocations)
                                     {
                                         return;
                                     }
                                     var mesh = push.skinnedMeshes[index];
                                     var vertex = mesh.vertices[0];
                                     var vertexCount = mesh.count;
                                     var bounds = Rin::Shade::Tests::Fixtures::UpdatableBounds3D(vertex.getLocation());
                                     for (var i = 1; i < vertexCount; i++)
                                     {
                                         vertex = mesh.vertices[i];
                                         bounds.update(vertex.getLocation());
                                     }
                                     push.output[mesh.index] = bounds;
                                 }

                                 """;

        Assert.That(result.Shaders["BoundsUpdateFixtureShader"].Replace("\r\n", "\n"),
            Is.EqualTo(expected.Replace("\r\n", "\n")));
    }
}
