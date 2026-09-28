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
        Assert.That(slang, Does.Contain("extension MeshData"));
        Assert.That(slang, Does.Contain("float getRed()"));
        Assert.That(slang, Does.Contain("return colorAndTextureId.x;"));

        // The call site is unaffected either way - already worked before this fix.
        Assert.That(slang, Does.Contain("push.output[0] = push.mesh.getRed();"));
    }
}
