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

                               public struct AtomicBindings
                               {
                                   public RWByteAddressBuffer Counters;
                               }

                               [Shader]
                               public class AtomicShader : Shader
                               {
                                   public struct AtomicPush
                                   {
                                       public uint Value;
                                   }

                                   [Push] protected AtomicPush Push;
                                   [BindingGroup] protected static AtomicBindings Bindings;
                                   [GroupShared] protected static uint Shared;
                                   [GroupShared] protected static int Signed;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       InterlockedAdd(ref Shared, Push.Value);
                                       InterlockedMin(ref Shared, Push.Value, out var previousMin);
                                       InterlockedMax(ref Signed, (int)Push.Value);
                                       Bindings.Counters.InterlockedOr(8u, 1u);
                                       Bindings.Counters.InterlockedExchange(12u, previousMin, out var swapped);
                                       Bindings.Counters.InterlockedCompareExchange(16u, swapped, Push.Value, out var seen);
                                       Bindings.Counters.Store(20u, seen);
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["AtomicShader"];
        Snapshot.Verify(slang);
        Assert.Multiple(() =>
        {
            Assert.That(slang, Does.Contain("InterlockedAdd(shared, push.value);"));
            Assert.That(slang, Does.Contain("InterlockedMin(shared, push.value, previousMin);"));
            Assert.That(slang, Does.Contain("InterlockedMax(signed, (int)push.value);"));
            Assert.That(slang, Does.Contain("bindings.counters.InterlockedOr(8u, 1u);"));
            Assert.That(slang, Does.Contain("bindings.counters.InterlockedExchange(12u, previousMin, swapped);"));
            Assert.That(slang, Does.Contain("bindings.counters.InterlockedCompareExchange(16u, swapped, push.value, seen);"));
            Assert.That(slang, Does.Contain("bindings.counters.Store<uint>(20u, seen);"));
        });
    }
}
