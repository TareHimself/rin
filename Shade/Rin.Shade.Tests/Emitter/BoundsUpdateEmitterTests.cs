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
                                 struct Vertex
                                 {
                                     float4 locationU;
                                     float4 normalV;
                                     float4 tangent;
                                 }

                                 struct SkinnedMesh
                                 {
                                     int index;
                                     Vertex* vertices;
                                     uint count;
                                 }

                                 struct UpdatableBounds3D
                                 {
                                     float3 lower;
                                     float3 upper;
                                 }

                                 struct BoundsUpdatePushConstants
                                 {
                                     SkinnedMesh* skinnedMeshes;
                                     int totalInvocations;
                                     UpdatableBounds3D* output;
                                 }

                                 struct ComputeIn
                                 {
                                     uint threadId : SV_DispatchThreadID;
                                 }

                                 extension UpdatableBounds3D
                                 {
                                     __init(float3 location)
                                     {
                                         lower = location;
                                         upper = location;
                                     }
                                 }

                                 extension Vertex
                                 {
                                     float3 getLocation()
                                     {
                                         return locationU.xyz;
                                     }
                                 }

                                 extension UpdatableBounds3D
                                 {
                                     [mutating]
                                     void update(float3 location)
                                     {
                                         lower = min(lower, location);
                                         upper = max(upper, location);
                                     }
                                 }

                                 [[vk::push_constant]] uniform ConstantBuffer<BoundsUpdatePushConstants, ScalarDataLayout> push;

                                 [shader("compute")]
                                 [numthreads(64, 1, 1)]
                                 void compute(ComputeIn input)
                                 {
                                     var index = input.threadId;
                                     if (index >= (uint)push.totalInvocations)
                                     {
                                         return;
                                     }
                                     var mesh = push.skinnedMeshes[index];
                                     var vertex = mesh.vertices[0];
                                     var vertexCount = mesh.count;
                                     var bounds = UpdatableBounds3D(vertex.getLocation());
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
