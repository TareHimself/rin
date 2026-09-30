using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Shade.Tests;
using Rin.Shade.Transpiler;
using Rin.Slang.Compiler;

namespace Rin.Shade.CpuTests;

/// <summary>
///     Runs a transpiled shader on the CPU and compares it with System.Numerics. The C# and Slang sides
///     must agree on matrix convention (row-major storage, row-vector math), and this is the one place
///     that checks the real compiled result instead of the emitted text.
/// </summary>
public class MatrixConventionTests
{
    private const string Source = """
        using System.Numerics;
        using Rin.Shade;

        namespace CpuCheck;

        public struct ConventionPush
        {
            public BufferRef<Matrix4x4> Models;
            public BufferRef<Matrix4x4> Views;
            public BufferRef<Matrix4x4> Projections;
            public BufferRef<Vector4> Points;
            public BufferRef<Vector4> Chained;
            public BufferRef<Vector4> Combined;
            public uint Count;
        }

        [Shader("Cpu/convention.slang")]
        public class ConventionShader : Shader
        {
            [Push] protected ConventionPush Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                for (var index = 0u; index < Push.Count; index++)
                {
                    var model = Push.Models[index];
                    var view = Push.Views[index];
                    var projection = Push.Projections[index];
                    var point = Push.Points[index];

                    Push.Chained[index] = Vector4.Transform(Vector4.Transform(Vector4.Transform(point, model), view), projection);

                    var combined = model * view * projection;
                    Push.Combined[index] = Vector4.Transform(point, combined);
                }
            }
        }
        """;

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct Push
    {
        public Matrix4x4* Models;
        public Matrix4x4* Views;
        public Matrix4x4* Projections;
        public Vector4* Points;
        public Vector4* Chained;
        public Vector4* Combined;
        public uint Count;
    }

    private static string Transpile()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));
        Assert.That(result.Diagnostics, Is.Empty);
        return result.Shaders["ConventionShader"];
    }

    [Test]
    public void TranspiledShaderCompilesToHostCode()
    {
        var slang = Transpile();

        using var compiler = new HostComputeCompiler();
        using var shader = compiler.Compile(slang);
    }

    [Test]
    public unsafe void TranspiledMatrixMathMatchesSystemNumerics()
    {
        Matrix4x4[] models =
        [
            Matrix4x4.CreateRotationY(0.7f) * Matrix4x4.CreateTranslation(1f, -2f, 3f),
            Matrix4x4.CreateScale(2f, 0.5f, 1.5f) * Matrix4x4.CreateRotationX(-0.4f) * Matrix4x4.CreateTranslation(-4f, 0.5f, 2f)
        ];
        Matrix4x4[] views =
        [
            Matrix4x4.CreateLookAt(new Vector3(0f, 1f, 6f), Vector3.Zero, Vector3.UnitY),
            Matrix4x4.CreateLookAt(new Vector3(3f, 2f, -5f), new Vector3(0f, 1f, 0f), Vector3.UnitY)
        ];
        Matrix4x4[] projections =
        [
            Matrix4x4.CreatePerspectiveFieldOfView(1.0f, 16f / 9f, 0.1f, 100f),
            Matrix4x4.CreatePerspectiveFieldOfView(0.8f, 1.5f, 0.3f, 50f)
        ];
        Vector4[] points = [new(1f, 2f, 3f, 1f), new(-2f, 0.5f, 4f, 1f)];
        var chained = new Vector4[2];
        var combined = new Vector4[2];

        using var compiler = new HostComputeCompiler();
        using var shader = compiler.Compile(Transpile());

        fixed (Matrix4x4* m = models)
        fixed (Matrix4x4* v = views)
        fixed (Matrix4x4* p = projections)
        fixed (Vector4* pts = points)
        fixed (Vector4* outChained = chained)
        fixed (Vector4* outCombined = combined)
        {
            var push = new Push
            {
                Models = m, Views = v, Projections = p, Points = pts, Chained = outChained, Combined = outCombined,
                Count = 2
            };
            shader.Dispatch(1, 1, 1, ref push);
        }

        for (var i = 0; i < 2; i++)
        {
            var expected = Vector4.Transform(Vector4.Transform(Vector4.Transform(points[i], models[i]), views[i]),
                projections[i]);
            var expectedCombined = Vector4.Transform(points[i], models[i] * views[i] * projections[i]);

            AssertClose(chained[i], expected, $"chained Transform, point {i}");
            AssertClose(combined[i], expectedCombined, $"matrix product then Transform, point {i}");
        }
    }

    private static void AssertClose(Vector4 actual, Vector4 expected, string what)
    {
        for (var c = 0; c < 4; c++)
        {
            var a = actual[c];
            var e = expected[c];
            Assert.That(a, Is.EqualTo(e).Within(Math.Max(1e-4f, Math.Abs(e) * 1e-4f)),
                $"{what}: component {c} was {a}, System.Numerics gives {e}");
        }
    }
}
