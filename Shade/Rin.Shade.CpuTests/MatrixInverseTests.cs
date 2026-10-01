using System.Numerics;
using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices;
using Rin.Shade.Tests;
using Rin.Shade.Transpiler;
using Rin.Slang.Compiler;

namespace Rin.Shade.CpuTests;

/// <summary>
///     MatrixMath.Inverse is one C# function that is both the CPU implementation and, transpiled, the GPU
///     one. This runs both against System.Numerics' own inverse.
/// </summary>
public class MatrixInverseTests
{
    private const string ShaderSource = """
        using System.Numerics;
        using Rin.Shade;

        namespace InverseCheck;

        public struct InversePush
        {
            public BufferRef<Matrix4x4> Input;
            public BufferRef<Matrix4x4> Output;
            public uint Count;
        }

        [Shader("Cpu/inverse.slang")]
        public class InverseShader : Shader
        {
            [Push] protected InversePush Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                for (var index = 0u; index < Push.Count; index++)
                {
                    Push.Output[index] = MatrixMath.Inverse(Push.Input[index]);
                }
            }
        }
        """;

    private static readonly Matrix4x4[] Inputs =
    [
        Matrix4x4.CreateRotationY(0.7f) * Matrix4x4.CreateTranslation(1f, -2f, 3f),
        Matrix4x4.CreateScale(2f, 0.5f, 1.5f) * Matrix4x4.CreateRotationX(-0.4f) * Matrix4x4.CreateTranslation(-4f, 0.5f, 2f),
        Matrix4x4.CreatePerspectiveFieldOfView(1.0f, 16f / 9f, 0.1f, 100f),
        Matrix4x4.CreateLookAt(new Vector3(3f, 2f, -5f), new Vector3(0f, 1f, 0f), Vector3.UnitY)
    ];

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct Push
    {
        public Matrix4x4* Input;
        public Matrix4x4* Output;
        public uint Count;
    }

    [Test]
    public void CSharpInverseMatchesSystemNumerics()
    {
        foreach (var input in Inputs)
        {
            Assert.That(Matrix4x4.Invert(input, out var expected), Is.True);
            AssertClose(MatrixMath.Inverse(input), expected);
        }
    }

    [Test]
    public unsafe void TranspiledInverseMatchesSystemNumerics()
    {
        // The same path the build task takes: MatrixMath's source arrives from Rin.Shade's exported sources.
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(Shader).Assembly.Location))
            .ToList();
        var scratch = ScratchCompilationBuilder.Build([ShaderSource], references, "InverseCheck");
        var result = ShadeEmitter.Emit(scratch.Compilation, scratch.LocalTrees);
        Assert.That(result.Diagnostics, Is.Empty);

        using var compiler = new HostComputeCompiler();
        using var shader = compiler.Compile(result.Shaders["InverseShader"]);

        var outputs = new Matrix4x4[Inputs.Length];
        fixed (Matrix4x4* input = Inputs)
        fixed (Matrix4x4* output = outputs)
        {
            var push = new Push { Input = input, Output = output, Count = (uint)Inputs.Length };
            shader.Dispatch(1, 1, 1, ref push);
        }

        for (var i = 0; i < Inputs.Length; i++)
        {
            Matrix4x4.Invert(Inputs[i], out var expected);
            AssertClose(outputs[i], expected);
        }
    }

    private static void AssertClose(Matrix4x4 actual, Matrix4x4 expected)
    {
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 4; column++)
        {
            var a = Element(actual, row, column);
            var e = Element(expected, row, column);
            Assert.That(a, Is.EqualTo(e).Within(Math.Max(1e-4f, Math.Abs(e) * 1e-4f)),
                $"element [{row},{column}] was {a}, System.Numerics gives {e}");
        }
    }

    private static float Element(Matrix4x4 m, int row, int column) => row switch
    {
        0 => new Vector4(m.M11, m.M12, m.M13, m.M14)[column],
        1 => new Vector4(m.M21, m.M22, m.M23, m.M24)[column],
        2 => new Vector4(m.M31, m.M32, m.M33, m.M34)[column],
        _ => new Vector4(m.M41, m.M42, m.M43, m.M44)[column]
    };
}
