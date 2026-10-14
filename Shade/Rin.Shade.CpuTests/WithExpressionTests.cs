using Rin.Shade.Tests;
using Rin.Shade.Transpiler;
using Rin.Slang.Compiler;

namespace Rin.Shade.CpuTests;

/// <summary>
///     Compiles the helpers that `with` expressions lower to with the real Slang compiler, since the emitter
///     tests only check the text.
/// </summary>
public class WithExpressionTests
{
    private const string Source = """
        using System.Numerics;
        using Rin.Shade;

        namespace WithCpuCheck;

        [ShaderStruct]
        public readonly record struct Bounds3D(Vector3 Lower, Vector3 Upper);

        [ShaderStruct]
        public struct Sized
        {
            public float Width;
            public float Height;

            public float Area
            {
                get => Width * Height;
                set
                {
                    Width = value;
                    Height = 1f;
                }
            }
        }

        public struct Push
        {
            public Bounds3D Bounds;
            public Sized Sized;
            public float NewArea;
            public BufferRef<Bounds3D> Bounds3DOutput;
            public BufferRef<Sized> SizedOutput;
        }

        [Shader]
        public class WithShader : Shader
        {
            [Push] protected Push Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                Push.Bounds3DOutput[0] = Push.Bounds with { Upper = Push.Bounds.Lower };
                Push.SizedOutput[0] = Push.Sized with { Area = Push.NewArea };
            }
        }
        """;

    [Test]
    public void WithOnRecordStructAndSetterPropertyCompiles()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));
        Assert.That(result.Diagnostics, Is.Empty);

        using var compiler = new HostComputeCompiler();
        using var shader = compiler.Compile(result.Shaders["WithShader"]);
    }
}
