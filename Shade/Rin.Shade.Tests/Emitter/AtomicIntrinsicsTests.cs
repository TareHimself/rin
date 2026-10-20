using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class AtomicIntrinsicsTests
{
    [Test]
    public void AtomicsLowerToInterlockedStatements()
    {
        const string source = """
                               using Rin.Shade;

                               namespace AtomicCheck;

                               [Shader]
                               public class AtomicShader : Shader
                               {
                                   public struct AtomicPush
                                   {
                                       public BufferRef<uint> Counters;
                                       public BufferRef<int> Signed;
                                       public uint Value;
                                   }

                                   [Push] protected AtomicPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       InterlockedAdd(ref Push.Counters[0], Push.Value);
                                       InterlockedMin(ref Push.Counters[1], Push.Value, out var previousMin);
                                       InterlockedMax(ref Push.Signed[0], (int)Push.Value);
                                       InterlockedOr(ref Push.Counters[2], 1u);
                                       InterlockedExchange(ref Push.Counters[3], previousMin, out var swapped);
                                       InterlockedCompareExchange(ref Push.Counters[4], swapped, Push.Value, out var seen);
                                       Push.Counters[5] = seen;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["AtomicShader"];
        Snapshot.Verify(slang);
        Assert.Multiple(() =>
        {
            Assert.That(slang, Does.Contain("InterlockedAdd(push.counters[0], push.value);"));
            Assert.That(slang, Does.Contain("InterlockedMin(push.counters[1], push.value, previousMin);"));
            Assert.That(slang, Does.Contain("InterlockedMax(push.signed[0], (int)push.value);"));
            Assert.That(slang, Does.Contain("InterlockedOr(push.counters[2], 1);"));
            Assert.That(slang, Does.Contain("InterlockedExchange(push.counters[3], previousMin, swapped);"));
            Assert.That(slang, Does.Contain("InterlockedCompareExchange(push.counters[4], swapped, push.value, seen);"));
        });
    }
}
