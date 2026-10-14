using Rin.Core.Shared.Video;

namespace Rin.Core.Tests.Shared.Video;

public class SparseByteBufferTests
{
    private static byte[] Bytes(int start, int count)
    {
        var data = new byte[count];
        for (var i = 0; i < count; i++) data[i] = (byte)(start + i);
        return data;
    }

    private static byte[] ReadBack(SparseByteBuffer buffer, long offset, int count)
    {
        var data = new byte[count];
        buffer.Read(offset, data);
        return data;
    }

    [Test]
    public void NothingIsAvailableBeforeAWrite()
    {
        var buffer = new SparseByteBuffer();

        Assert.That(buffer.Available, Is.EqualTo(0));
        Assert.That(buffer.AvailableFrom(10), Is.EqualTo(10));
        Assert.That(buffer.HeldBytes, Is.EqualTo(0));
    }

    [Test]
    public void AppendsGrowTheRunFromTheStart()
    {
        var buffer = new SparseByteBuffer();
        buffer.Write(0, Bytes(0, 4));
        buffer.Write(4, Bytes(4, 4));

        Assert.That(buffer.Available, Is.EqualTo(8));
        Assert.That(ReadBack(buffer, 2, 4), Is.EqualTo(Bytes(2, 4)));
    }

    [Test]
    public void APieceWrittenAheadIsHeldOnItsOwn()
    {
        var buffer = new SparseByteBuffer();
        buffer.Write(100, Bytes(100, 10));

        Assert.That(buffer.Available, Is.EqualTo(0));
        Assert.That(buffer.AvailableFrom(100), Is.EqualTo(110));
        Assert.That(buffer.AvailableFrom(105), Is.EqualTo(110));
        Assert.That(buffer.AvailableFrom(110), Is.EqualTo(110));
        Assert.That(ReadBack(buffer, 103, 3), Is.EqualTo(Bytes(103, 3)));
    }

    [Test]
    public void WritingTheGapJoinsThePiecesAroundIt()
    {
        var buffer = new SparseByteBuffer();
        buffer.Write(0, Bytes(0, 4));
        buffer.Write(8, Bytes(8, 4));
        buffer.Write(4, Bytes(4, 4));

        Assert.That(buffer.Available, Is.EqualTo(12));
        Assert.That(buffer.HeldBytes, Is.EqualTo(12));
        Assert.That(ReadBack(buffer, 0, 12), Is.EqualTo(Bytes(0, 12)));
    }

    [Test]
    public void AWriteOverlappingTwoPiecesMergesThem()
    {
        var buffer = new SparseByteBuffer();
        buffer.Write(0, Bytes(0, 6));
        buffer.Write(10, Bytes(10, 6));
        buffer.Write(4, Bytes(4, 8));

        Assert.That(buffer.AvailableFrom(0), Is.EqualTo(16));
        Assert.That(buffer.HeldBytes, Is.EqualTo(16));
        Assert.That(ReadBack(buffer, 0, 16), Is.EqualTo(Bytes(0, 16)));
    }

    [Test]
    public void AWriteBeforeAPieceThatReachesItMerges()
    {
        var buffer = new SparseByteBuffer();
        buffer.Write(8, Bytes(8, 4));
        buffer.Write(4, Bytes(4, 4));

        Assert.That(buffer.AvailableFrom(4), Is.EqualTo(12));
        Assert.That(buffer.HeldBytes, Is.EqualTo(8));
        Assert.That(ReadBack(buffer, 4, 8), Is.EqualTo(Bytes(4, 8)));
    }

    [Test]
    public void ReadingBytesThatAreNotHeldThrows()
    {
        var buffer = new SparseByteBuffer();
        buffer.Write(0, Bytes(0, 4));

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Read(2, new byte[4]));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Read(10, new byte[1]));
    }

    [Test]
    public void MissingRangesSkipWhatIsHeld()
    {
        var buffer = new SparseByteBuffer();
        buffer.Write(10, Bytes(0, 10));
        buffer.Write(30, Bytes(0, 10));
        var missing = new List<ByteRange>();

        buffer.CollectMissing(0, 50, missing);

        Assert.That(missing, Is.EqualTo(new[] { new ByteRange(0, 10), new ByteRange(20, 30), new ByteRange(40, 50) }));
    }

    [Test]
    public void NothingIsMissingInsideAPiece()
    {
        var buffer = new SparseByteBuffer();
        buffer.Write(0, Bytes(0, 20));
        var missing = new List<ByteRange>();

        buffer.CollectMissing(5, 15, missing);

        Assert.That(missing, Is.Empty);
    }

    [Test]
    public void ReleasingDropsPiecesBelowAndKeepsTheRunAvailable()
    {
        var buffer = new SparseByteBuffer();
        buffer.Write(0, Bytes(0, 10));
        buffer.Write(20, Bytes(20, 10));

        buffer.ReleaseBefore(25);

        Assert.That(buffer.Released, Is.EqualTo(25));
        Assert.That(buffer.AvailableFrom(0), Is.EqualTo(0));
        Assert.That(buffer.Available, Is.EqualTo(30));
        Assert.That(ReadBack(buffer, 25, 5), Is.EqualTo(Bytes(25, 5)));
        Assert.That(buffer.HeldBytes, Is.LessThanOrEqualTo(10));
    }

    [Test]
    public void AReleaseBehindTheLastOneIsIgnored()
    {
        var buffer = new SparseByteBuffer();
        buffer.Write(0, Bytes(0, 10));
        buffer.ReleaseBefore(8);

        buffer.ReleaseBefore(2);

        Assert.That(buffer.Released, Is.EqualTo(8));
        Assert.That(buffer.Available, Is.EqualTo(10));
    }

    [Test]
    public void AppendingAfterAReleaseStillReadsBack()
    {
        var buffer = new SparseByteBuffer();
        buffer.Write(0, Bytes(0, 100));
        buffer.ReleaseBefore(90);
        buffer.Write(100, Bytes(100, 20));

        Assert.That(buffer.Available, Is.EqualTo(120));
        Assert.That(ReadBack(buffer, 90, 30), Is.EqualTo(Bytes(90, 30)));
    }
}
