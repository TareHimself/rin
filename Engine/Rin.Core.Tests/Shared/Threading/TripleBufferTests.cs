using Rin.Core.Shared.Threading;

namespace Rin.Core.Tests.Shared.Threading;

public class TripleBufferTests
{
    private class Box(int value)
    {
        public readonly int Value = value;
    }

    [Test]
    public void TryReadBeforeAnyWriteReturnsFalse()
    {
        var buffer = new TripleBuffer<Box>();

        var hasNewValue = buffer.TryRead(out var value);

        Assert.That(hasNewValue, Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void TryReadAfterWriteReturnsThatValue()
    {
        var buffer = new TripleBuffer<Box>();
        var written = new Box(1);

        buffer.Write(written);
        var hasNewValue = buffer.TryRead(out var value);

        Assert.That(hasNewValue, Is.True);
        Assert.That(value, Is.SameAs(written));
    }

    [Test]
    public void TryReadWithoutNewWriteReturnsFalseAndTheSamePreviousValue()
    {
        var buffer = new TripleBuffer<Box>();
        var written = new Box(1);
        buffer.Write(written);
        buffer.TryRead(out _);

        var hasNewValue = buffer.TryRead(out var value);

        Assert.That(hasNewValue, Is.False);
        Assert.That(value, Is.SameAs(written));
    }

    [Test]
    public void SecondWriteBeforeAnyReadReportsTheFirstValueAsDiscarded()
    {
        var buffer = new TripleBuffer<Box>();
        var first = new Box(1);
        var second = new Box(2);

        var discardedOnFirstWrite = buffer.Write(first);
        var discardedOnSecondWrite = buffer.Write(second);

        Assert.That(discardedOnFirstWrite, Is.Null);
        Assert.That(discardedOnSecondWrite, Is.SameAs(first));
    }

    [Test]
    public void ReaderSkipsStraightToTheLatestValueWhenItFallsBehind()
    {
        var buffer = new TripleBuffer<Box>();
        buffer.Write(new Box(1));
        var latest = new Box(2);
        buffer.Write(latest);

        var hasNewValue = buffer.TryRead(out var value);

        Assert.That(hasNewValue, Is.True);
        Assert.That(value, Is.SameAs(latest));
    }

    [Test]
    public void RemainingValuesIncludesTheMostRecentlyWrittenValue()
    {
        var buffer = new TripleBuffer<Box>();
        var written = new Box(1);

        buffer.Write(written);

        Assert.That(buffer.RemainingValues, Has.Some.SameAs(written));
    }
}
