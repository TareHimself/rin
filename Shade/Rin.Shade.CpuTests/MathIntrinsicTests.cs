using System.Runtime.InteropServices;
using Rin.Shade.Tests;
using Rin.Shade.Transpiler;
using Rin.Slang.Compiler;

namespace Rin.Shade.CpuTests;

/// <summary>
///     Runs the same C# through Slang and as ordinary C#, so the body of each math builtin is held to what the builtin
///     does in a shader.
/// </summary>
public class MathIntrinsicTests
{
    private const string Source = """
        using Rin.Shade;
        using Rin.Shade.CpuTests;
        using Rin.Shade.Tests;

        namespace MathCpuCheck;

        public struct Push
        {
            public TestBuffer<float> X;
            public TestBuffer<float> Y;
            public TestBuffer<float> Output;
        }

        [Shader]
        public class MathShader : Shader
        {
            [Push] protected Push Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                for (var i = 0; i < 6; i++)
                for (var id = 0; id < MathIntrinsicCases.Count; id++)
                    Push.Output[i * MathIntrinsicCases.Count + id] =
                        MathIntrinsicCases.Evaluate(id, Push.X[i], Push.Y[i]);
            }
        }
        """;

    private const float Tolerance = 1e-4f;

    private static readonly float[] X = [0.07f, 0.23f, 0.41f, 0.58f, 0.76f, 0.93f];
    private static readonly float[] Y = [0.41f, 0.93f, 0.07f, 0.76f, 0.23f, 0.58f];

    [StructLayout(LayoutKind.Sequential)]
    private struct Push
    {
        public TestBuffer<float> X;
        public TestBuffer<float> Y;
        public TestBuffer<float> Output;
    }

    private static float[] RunThroughSlang()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build([Source, FixtureSource.Read("MathIntrinsicCases.cs")]));
        Assert.That(result.Diagnostics, Is.Empty);

        using var compiler = new HostComputeCompiler();
        using var shader = compiler.Compile(result.Shaders["MathShader"]);

        var output = new float[X.Length * MathIntrinsicCases.Count];
        using var buffers = new PinnedBuffers();
        var push = new Push { X = buffers.Add(X), Y = buffers.Add(Y), Output = buffers.Add(output) };
        shader.Dispatch(1, 1, 1, buffers.Heap, ref push);
        return output;
    }

    [Test]
    public void EveryMathBuiltinGivesTheSameNumberInCSharpAsInSlang()
    {
        var fromSlang = RunThroughSlang();

        Assert.Multiple(() =>
        {
            for (var i = 0; i < X.Length; i++)
            for (var id = 0; id < MathIntrinsicCases.Count; id++)
            {
                var expected = fromSlang[i * MathIntrinsicCases.Count + id];
                var tolerance = Tolerance * float.Max(1f, float.Abs(expected));
                Assert.That(MathIntrinsicCases.Evaluate(id, X[i], Y[i]), Is.EqualTo(expected).Within(tolerance),
                    $"case {id} for x = {X[i]}, y = {Y[i]}");
            }
        });
    }
}
