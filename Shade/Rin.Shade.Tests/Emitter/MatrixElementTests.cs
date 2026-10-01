using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class MatrixElementTests
{
    [Test]
    public void RowAndColumnLowerToRowIndexingAndTranspose()
    {
        const string source = """
            using System.Numerics;
            using Rin.Shade;

            namespace MatrixElementCheck;

            public struct MatrixPush
            {
                public Matrix4x4 Matrix;
                public BufferRef<float> Output;
            }

            [Shader("Fixtures/matrix_element.slang")]
            public class MatrixElementShader : Shader
            {
                [Push] protected MatrixPush Push;

                [Compute(1, 1, 1)]
                public void Compute()
                {
                    var local = Push.Matrix;
                    Push.Output[0] = Push.Matrix.Row(1).X + local.Column(2).Y + local.Row(3).W;
                }
            }
            """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["MatrixElementShader"];
        Assert.That(slang, Does.Contain("push.matrix[1].x"));
        Assert.That(slang, Does.Contain("transpose(local)[2].y"));
        Assert.That(slang, Does.Contain("local[3].w"));
    }
}
