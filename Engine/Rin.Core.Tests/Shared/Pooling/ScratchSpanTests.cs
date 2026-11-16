using System.Runtime.CompilerServices;
using Rin.Core.Shared.Pooling;

namespace Rin.Core.Tests.Shared.Pooling;

public class ScratchSpanTests
{
    [Test]
    public void AStackBufferThatFitsIsUsedAtTheExactLength()
    {
        Span<int> stack = stackalloc int[8];

        using var scratch = new ScratchSpan<int>(5, stack);
        var length = scratch.Span.Length;
        var usesStack = Unsafe.AreSame(ref scratch.Span[0], ref stack[0]);

        Assert.That(length, Is.EqualTo(5));
        Assert.That(usesStack, "uses the stack buffer");
    }

    [Test]
    public void WithoutAStackBufferItRentsAtTheExactLength()
    {
        using var scratch = new ScratchSpan<int>(100, default);

        Assert.That(scratch.Span.Length, Is.EqualTo(100));
    }

    [Test]
    public void ARentedSpanIsZeroedEvenWhenThePoolHandsBackDirtyMemory()
    {
        using (var dirty = new ScratchSpan<long>(64, default)) dirty.Span.Fill(-1);

        using var scratch = new ScratchSpan<long>(64, default);

        Assert.That(scratch.Span.ToArray(), Is.All.Zero);
    }

    [Test]
    public void AStackSpanIsZeroed()
    {
        Span<long> stack = stackalloc long[4];
        stack.Fill(-1);

        using var scratch = new ScratchSpan<long>(4, stack);

        Assert.That(scratch.Span.ToArray(), Is.All.Zero);
    }

    [Test]
    public void TheConditionalStackallocFormCompilesAndWorksBothWays()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Sum(3), Is.EqualTo(3));
            Assert.That(Sum(300), Is.EqualTo(300));
        });

        static int Sum(int count)
        {
            using var scratch = new ScratchSpan<int>(count, count <= 8 ? stackalloc int[count] : default);
            scratch.Span.Fill(1);
            var sum = 0;
            foreach (var value in scratch.Span) sum += value;
            return sum;
        }
    }
}
