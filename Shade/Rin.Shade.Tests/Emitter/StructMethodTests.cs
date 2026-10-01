namespace Rin.Shade.Tests.Emitter;

using Rin.Shade.Transpiler;

public class StructMethodTests
{
    [Test]
    public void StructInstanceMethodEmitsAsASlangExtensionNotABareFreeFunction()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace StructMethodCheck;

                               [ShaderStruct]
                               public struct MeshData
                               {
                                   public Vector4 ColorAndTextureId;

                                   public float GetRed() => ColorAndTextureId.X;
                               }

                               public struct StructMethodPush
                               {
                                   public MeshData Mesh;
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/struct_method.slang")]
                               public class StructMethodShader : Shader
                               {
                                   [Push] protected StructMethodPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = Push.Mesh.GetRed();
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["StructMethodShader"];

        // The method definition is nested in an extension, not a bare top-level function - a bare
        // function referencing "colorAndTextureId" with no enclosing struct/extension scope would
        // be invalid Slang (undefined identifier).
        Assert.That(slang, Does.Contain("extension StructMethodCheck::MeshData"));
        Assert.That(slang, Does.Contain("float getRed()"));
        Assert.That(slang, Does.Contain("return this.colorAndTextureId.x;"));

        // The call site is unaffected either way - already worked before this fix.
        Assert.That(slang, Does.Contain("push.output[0] = push.mesh.getRed();"));
    }

    [Test]
    public void ConstructorParameterNamedLikeAFieldDoesNotShadowIt()
    {
        const string source = """
                               using Rin.Shade;

                               namespace ShadowCheck;

                               public struct Range
                               {
                                   public uint Start;
                                   public uint Count;

                                   public Range(uint start, uint count)
                                   {
                                       Start = start;
                                       Count = count;
                                   }
                               }

                               public struct RangePush
                               {
                                   public BufferRef<uint> Output;
                               }

                               [Shader("Fixtures/shadow.slang")]
                               public class ShadowShader : Shader
                               {
                                   [Push] protected RangePush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var range = new Range(2u, 5u);
                                       Push.Output[0] = range.Start + range.Count;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["ShadowShader"];
        Assert.That(slang, Does.Contain("this.start = start;"));
        Assert.That(slang, Does.Contain("this.count = count;"));
    }
}
