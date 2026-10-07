using System.Runtime.InteropServices;
using Rin.Slang.Compiler;

namespace Rin.Shade.CpuTests;

/// <summary>
///     Every block a shader reads is scalar layout: a float3 after a float sits at offset 4, where std140 and
///     std430 would put it at 16. Read from the Offset decorations of the compiled SPIR-V.
/// </summary>
public class ScalarLayoutTests
{
    private const int OpName = 5;
    private const int OpMemberDecorate = 72;
    private const int DecorationOffset = 35;
    private const int SpirvHeaderWords = 5;

    private const string ShaderSource = """
        struct Probe
        {
            float a;
            float3 b;
        }

        struct Push
        {
            Probe* data;
        }

        struct Block
        {
            Probe value;
        }

        [[vk::push_constant]] uniform ConstantBuffer<Push, ScalarDataLayout> push;
        ParameterBlock<Block> block;

        [shader("compute")]
        [numthreads(1, 1, 1)]
        void compute()
        {
            push.data[0].b = block.value.b + float3(push.data[1].a);
        }
        """;

    [Test]
    public void PointerAndParameterBlockStructsUseScalarOffsets()
    {
        var offsets = ProbeSecondMemberOffsets(CompileToSpirv(ShaderSource));

        Assert.That(offsets, Is.Not.Empty);
        foreach (var (name, offset) in offsets) Assert.That(offset, Is.EqualTo(4), name);
    }

    private static byte[] CompileToSpirv(string source)
    {
        var root = Directory.CreateTempSubdirectory("rin-scalar-layout-").FullName;
        try
        {
            var path = Path.Combine(root, "probe.slang");
            File.WriteAllText(path, source);
            using var compiler = new ShaderCompiler(new ShaderCompilerOptions().SetPortableRoot(root));
            return compiler.Compile(path).Stages[0].Spirv;
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    /// <summary>
    ///     The offset of member 1 in every struct type whose debug name contains "Probe".
    /// </summary>
    private static List<(string Name, int Offset)> ProbeSecondMemberOffsets(byte[] spirv)
    {
        var words = MemoryMarshal.Cast<byte, uint>(spirv);
        var probeNames = new Dictionary<uint, string>();
        var secondMemberOffsets = new Dictionary<uint, int>();

        for (var i = SpirvHeaderWords; i < words.Length;)
        {
            var opcode = (int)(words[i] & 0xFFFF);
            var wordCount = (int)(words[i] >> 16);

            if (opcode == OpName && ReadString(words[(i + 2)..(i + wordCount)]) is var name && name.Contains("Probe"))
                probeNames[words[i + 1]] = name;

            if (opcode == OpMemberDecorate && words[i + 2] == 1 && words[i + 3] == DecorationOffset)
                secondMemberOffsets[words[i + 1]] = (int)words[i + 4];

            i += wordCount;
        }

        var offsets = new List<(string, int)>();
        foreach (var (id, name) in probeNames)
            if (secondMemberOffsets.TryGetValue(id, out var offset))
                offsets.Add((name, offset));

        return offsets;
    }

    private static string ReadString(ReadOnlySpan<uint> words)
    {
        var bytes = MemoryMarshal.AsBytes(words);
        var length = bytes.IndexOf((byte)0);
        return System.Text.Encoding.UTF8.GetString(length < 0 ? bytes : bytes[..length]);
    }
}
