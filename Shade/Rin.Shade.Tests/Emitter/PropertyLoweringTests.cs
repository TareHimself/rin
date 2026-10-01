using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class PropertyLoweringTests
{
    [Test]
    public void ComputedGetOnlyPropertyLowersToACallOfItsGetter()
    {
        // Shaped like Rin.Core.Graphics.DeviceHandle on purpose: a private const shift/mask (static,
        // so StructLowering excludes it from the struct body - only inlining its value at the
        // reference in getId() keeps this valid Slang) and a type used ONLY as a local variable,
        // never as a push-constant field or entry-point parameter/return type (only reachability
        // TypeGraph (formerly TypeCollector) didn't originally walk - nothing else in this fixture references PackedHandle
        // at all).
        const string source = """
                               using Rin.Shade;

                               namespace PropertyLoweringCheck;

                               [ShaderStruct]
                               public struct PackedHandle
                               {
                                   private const uint IdMask = 0xFFFFFF;

                                   public uint Data;

                                   public uint Id => (Data >> 8) & IdMask;
                               }

                               public struct ComputeIn
                               {
                                   [Semantic("SV_DispatchThreadID")] public uint ThreadId;
                               }

                               public struct PropertyLoweringPushConstants
                               {
                                   public uint Output;
                               }

                               [Shader("Fixtures/property_lowering.slang")]
                               public class PropertyLoweringShader : Shader
                               {
                                   [Push] protected PropertyLoweringPushConstants Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute(ComputeIn input)
                                   {
                                       PackedHandle handle;
                                       handle.Data = input.ThreadId;
                                       Push.Output = handle.Id;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["PropertyLoweringShader"].Replace("\r\n", "\n");

        // PackedHandle is never a push-constant field or entry-point parameter/return type - it's
        // only ever a local variable inside Compute(). Its struct declaration must still be emitted.
        Assert.That(slang, Does.Contain("    struct PackedHandle\n    {\n        uint data;\n    }"));
        Assert.That(slang, Does.Contain("uint getId()"));
        // IdMask has no Slang declaration of its own (static fields aren't struct members) - its
        // compile-time value must be inlined, not referenced by a qualified name pointing at nothing.
        Assert.That(slang, Does.Contain("return this.data >> 8 & 16777215;"));
        Assert.That(slang, Does.Contain("handle.getId()"));
    }
}
