using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Shade.Tests;
using Rin.Shade.Transpiler;
using Rin.Slang.Compiler;

namespace Rin.Shade.CpuTests;

/// <summary>
///     Runs bindless buffer element access on the CPU, so element strides and field offsets are checked against real
///     memory rather than against emitted text.
/// </summary>
public class BufferAccessTests
{
    private const string Source = """
        using System.Numerics;
        using Rin.Shade;
        using Rin.Shade.Tests;

        namespace BufferCpuCheck;

        public struct Item
        {
            public Vector4 Color;
            public Vector3 Position;
            public uint Count;
        }

        public struct Push
        {
            public TestBuffer<Item> Items;
            public TestBuffer<float> Output;
        }

        [Shader]
        public class BufferShader : Shader
        {
            [Push] protected Push Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                var item = Push.Items[1];
                item.Count = item.Count + 2u;
                item.Position.Y = 5f;
                Push.Items[1] = item;
                Push.Output[1] = Push.Items[1].Color.X + Push.Items[0].Position.Z;
            }
        }
        """;

    [StructLayout(LayoutKind.Sequential)]
    private struct Item
    {
        public Vector4 Color;
        public Vector3 Position;
        public uint Count;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Push
    {
        public TestBuffer<Item> Items;
        public TestBuffer<float> Output;
    }

    [Test]
    public void ElementsAreReadAndWrittenAtTheirCSharpOffsets()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));
        Assert.That(result.Diagnostics, Is.Empty);

        using var compiler = new HostComputeCompiler();
        using var shader = compiler.Compile(result.Shaders["BufferShader"]);

        Item[] items =
        [
            new() { Color = new Vector4(1f, 2f, 3f, 4f), Position = new Vector3(5f, 6f, 7f), Count = 1 },
            new() { Color = new Vector4(8f, 9f, 10f, 11f), Position = new Vector3(12f, 13f, 14f), Count = 2 }
        ];
        var output = new float[2];
        using (var buffers = new PinnedBuffers())
        {
            var push = new Push { Items = buffers.Add(items), Output = buffers.Add(output) };
            shader.Dispatch(1, 1, 1, buffers.Heap, ref push);
        }

        Assert.Multiple(() =>
        {
            Assert.That(items[0].Color, Is.EqualTo(new Vector4(1f, 2f, 3f, 4f)));
            Assert.That(items[0].Position, Is.EqualTo(new Vector3(5f, 6f, 7f)));
            Assert.That(items[0].Count, Is.EqualTo(1u));
            Assert.That(items[1].Color, Is.EqualTo(new Vector4(8f, 9f, 10f, 11f)));
            Assert.That(items[1].Position, Is.EqualTo(new Vector3(12f, 5f, 14f)));
            Assert.That(items[1].Count, Is.EqualTo(4u));
            Assert.That(output, Is.EqualTo(new[] { 0f, 8f + 7f }));
        });
    }
}
