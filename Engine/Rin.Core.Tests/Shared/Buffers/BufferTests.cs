using Rin.Core.Shared.Buffers;

namespace Rin.Core.Tests.Shared.Buffers;

public class BufferTests
{
    [Test]
    public void SpanConstructorCopiesTheElements()
    {
        int[] source = [1, 2, 3, 4];

        using var buffer = new Buffer<int>(source);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.ElementCount, Is.EqualTo(4));
            Assert.That(buffer.ByteSize, Is.EqualTo(16UL));
            Assert.That(buffer.AsReadOnlySpan().ToArray(), Is.EqualTo(source));
        });
    }

    [Test]
    public void SpanConstructorDoesNotAliasTheSource()
    {
        int[] source = [1, 2, 3];
        using var buffer = new Buffer<int>(source);

        source[0] = 99;

        Assert.That(buffer.GetElement(0), Is.EqualTo(1));
    }

    [Test]
    public void ZeroClearsEveryElement()
    {
        using var buffer = new Buffer<int>([7, 8, 9]);

        buffer.Zero();

        Assert.That(buffer.AsReadOnlySpan().ToArray(), Is.EqualTo(new[] { 0, 0, 0 }));
    }

    [Test]
    public void EmptyBufferHasNoElementsAndDisposesCleanly()
    {
        var buffer = new Buffer<int>();

        Assert.Multiple(() =>
        {
            Assert.That(buffer.ElementCount, Is.Zero);
            Assert.That(buffer.ByteSize, Is.Zero);
            Assert.DoesNotThrow(buffer.Dispose);
        });
    }

    [Test]
    public void BinarySerializeRoundTripsThroughBinaryDeserialize()
    {
        using var source = new Buffer<float>([1.5f, -2f, 3.25f]);
        using var stream = new MemoryStream();
        source.BinarySerialize(stream);
        stream.Position = 0;

        using var target = new Buffer<float>([0f]);
        target.BinaryDeserialize(stream);

        Assert.Multiple(() =>
        {
            Assert.That(target.ElementCount, Is.EqualTo(3));
            Assert.That(target.AsReadOnlySpan().ToArray(), Is.EqualTo(source.AsReadOnlySpan().ToArray()));
        });
    }
}
