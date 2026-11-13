using Rin.Core.Shared;

namespace Rin.Core.Tests.Shared;

public class PooledMemoryTests
{
    [Test]
    public void TruncateShortensTheVisibleSpan()
    {
        using var memory = PooledMemory<int>.CopyFrom([1, 2, 3, 4]);

        memory.Truncate(2);

        Assert.That(memory.Count, Is.EqualTo(2));
        Assert.That(memory.AsSpan().ToArray(), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void TruncateCannotGrow()
    {
        var memory = new PooledMemory<int>(2);

        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Truncate(3));
        memory.Dispose();
    }
}
