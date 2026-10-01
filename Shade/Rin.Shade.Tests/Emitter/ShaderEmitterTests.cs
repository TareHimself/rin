using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class ShaderEmitterTests
{
    [Test]
    public void EmitsCullingFixtureShader()
    {
        var source = FixtureSource.Read("../Fixtures/CullingFixtureShader.cs");
        var compilation = CompilationBuilder.Build(source);

        var result = ShadeEmitter.Emit(compilation);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders.Keys, Is.EquivalentTo(new[] { "CullingFixtureShader" }));

        const string expected = """
                                 namespace Rin::Shade::Tests::Fixtures
                                 {
                                     struct Bounds3D
                                     {
                                         float3 min;
                                         float3 max;
                                     }

                                     struct CullingPushConstants
                                     {
                                         Rin::Shade::Tests::Fixtures::Bounds3D* bounds;
                                         uint invocationCount;
                                         uint* output;
                                     }

                                     struct ComputeIn
                                     {
                                         uint threadId : SV_DispatchThreadID;
                                     }

                                 }

                                 [[vk::push_constant]] uniform ConstantBuffer<Rin::Shade::Tests::Fixtures::CullingPushConstants, ScalarDataLayout> push;

                                 [shader("compute")]
                                 [numthreads(64, 1, 1)]
                                 void compute(Rin::Shade::Tests::Fixtures::ComputeIn input)
                                 {
                                     if (input.threadId >= push.invocationCount)
                                     {
                                         return;
                                     }
                                     var bounds = push.bounds[(int)input.threadId];
                                     push.output[(int)input.threadId] = 1;
                                 }

                                 """;

        Assert.That(result.Shaders["CullingFixtureShader"].Replace("\r\n", "\n"), Is.EqualTo(expected.Replace("\r\n", "\n")));
    }

    [Test]
    public void RejectedConstructProducesLocatedDiagnostic()
    {
        const string source = """
                               using Rin.Shade;

                               namespace RejectedConstruct;

                               [Shader("Fixtures/rejected.slang")]
                               public class RejectedConstructShader : Shader
                               {
                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       try
                                       {
                                       }
                                       catch
                                       {
                                       }
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedConstruct.Id), Is.True);
    }

    [Test]
    public void SlangCallStubIsSubstitutedIntoTheCallSite()
    {
        const string source = """
                               using Rin.Shade;

                               namespace SlangCallMechanism;

                               public static class TestIntrinsics
                               {
                                   [SlangCall("max($0, $1)")]
                                   public static extern float Max(float a, float b);
                               }

                               public struct MaxPush
                               {
                                   public float A;
                                   public float B;
                               }

                               [Shader("Fixtures/max.slang")]
                               public class MaxShader : Shader
                               {
                                   [Push] protected MaxPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var result = TestIntrinsics.Max(Push.A, Push.B);
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders["MaxShader"], Does.Contain("var result = max(push.a, push.b);"));
    }
}
