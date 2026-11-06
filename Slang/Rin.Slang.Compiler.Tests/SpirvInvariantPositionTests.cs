using System.Runtime.InteropServices;

namespace Rin.Slang.Compiler.Tests;

public class SpirvInvariantPositionTests
{
    private static readonly uint[] Header = [0x07230203, 0x00010500, 0, 10, 0];

    private static uint Op(uint wordCount, uint opcode) => (wordCount << 16) | opcode;

    private static byte[] Module(params uint[][] instructions) =>
        MemoryMarshal.AsBytes(Header.Concat(instructions.SelectMany(i => i)).ToArray().AsSpan()).ToArray();

    private static uint[] Words(byte[] spirv) => MemoryMarshal.Cast<byte, uint>(spirv).ToArray();

    [Test]
    public void InsertsInvariantRightAfterThePositionDecoration()
    {
        uint[] location = [Op(4, 71), 7, 30, 0];
        uint[] position = [Op(4, 71), 9, 11, 0];
        uint[] binding = [Op(4, 71), 5, 33, 0];

        var patched = Words(SpirvInvariantPosition.Apply(Module(location, position, binding)));

        Assert.That(patched, Is.EqualTo(Header.Concat(location).Concat(position).Concat([Op(3, 71), 9u, 18u])
            .Concat(binding)));
    }

    [Test]
    public void LeavesAModuleWithoutAPositionOutputUnchanged()
    {
        var module = Module([Op(4, 71), 7, 30, 0], [Op(4, 71), 4, 11, 15]);

        Assert.That(SpirvInvariantPosition.Apply(module), Is.EqualTo(module));
    }
}
