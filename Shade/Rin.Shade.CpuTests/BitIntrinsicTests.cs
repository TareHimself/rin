using System.Runtime.InteropServices;
using Rin.Shade.Tests;
using Rin.Shade.Transpiler;
using Rin.Slang.Compiler;

namespace Rin.Shade.CpuTests;

/// <summary>
///     Runs <c>FirstBitLow</c> and <c>CountBits</c> through the real Slang compiler, since the clustered light loop walks
///     a bitmask with them.
/// </summary>
public class BitIntrinsicTests
{
    private const string Source = """
        using Rin.Shade;

        namespace BitCpuCheck;

        public struct Push
        {
            public BufferRef<uint> Input;
            public BufferRef<uint> Output;
        }

        [Shader]
        public class FirstBitLowShader : Shader
        {
            [Push] protected Push Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                for (var i = 0; i < 4; i++) Push.Output[i] = FirstBitLow(Push.Input[i]);
            }
        }

        [Shader]
        public class CountBitsShader : Shader
        {
            [Push] protected Push Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                for (var i = 0; i < 4; i++) Push.Output[i] = CountBits(Push.Input[i]);
            }
        }
        """;

    private static readonly uint[] Inputs = [0b1011_0000, 1u, 0x8000_0000, 0xFFFF_FFFF];

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct Push
    {
        public uint* Input;
        public uint* Output;
    }

    private static string Emit(string shaderName)
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));
        Assert.That(result.Diagnostics, Is.Empty);
        return result.Shaders[shaderName];
    }

    private static unsafe uint[] RunOnTheCpu(string shaderName)
    {
        using var compiler = new HostComputeCompiler();
        using var shader = compiler.Compile(Emit(shaderName));

        var output = new uint[Inputs.Length];
        fixed (uint* inputPointer = Inputs)
        fixed (uint* outputPointer = output)
        {
            var push = new Push { Input = inputPointer, Output = outputPointer };
            shader.Dispatch(1, 1, 1, ref push);
        }

        return output;
    }

    [Test]
    [Ignore("Slang 2025.9.2's CPU prelude has no U32_firstbitlow. Remove after the Slang bump.")]
    public void FirstBitLowMatchesTheBclOnTheCpu()
    {
        var expected = Inputs.Select(uint.TrailingZeroCount);
        Assert.That(RunOnTheCpu("FirstBitLowShader"), Is.EqualTo(expected));
    }

    [Test]
    public void FirstBitLowCompilesToSpirv()
    {
        var root = Directory.CreateTempSubdirectory("rin-shade-bits-").FullName;
        try
        {
            var path = Path.Combine(root, "first_bit_low.slang");
            File.WriteAllText(path, Emit("FirstBitLowShader"));
            var options = new ShaderCompilerOptions();
            options.AddSearchPath(root);
            options.SetPortableRoot(root);
            using var compiler = new ShaderCompiler(options);

            Assert.That(compiler.TryCompile(path, out _), Is.True);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void CountBitsMatchesTheBclOnTheCpu()
    {
        var expected = Inputs.Select(uint.PopCount);
        Assert.That(RunOnTheCpu("CountBitsShader"), Is.EqualTo(expected));
    }
}
