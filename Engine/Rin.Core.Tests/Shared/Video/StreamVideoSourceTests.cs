using Rin.Core.Shared.Video;

namespace Rin.Core.Tests.Shared.Video;

public class StreamVideoSourceTests
{
    [Test]
    public void LengthIsUnknownUntilTheStreamEnds()
    {
        var source = new StreamVideoSource();
        source.Append([1, 2, 3]);

        Assert.That(source.Length, Is.EqualTo(-1));
        Assert.That(source.Available, Is.EqualTo(3));

        source.End();

        Assert.That(source.Length, Is.EqualTo(3));
    }

    [Test]
    public void AppendsAreReadBackInOrder()
    {
        var source = new StreamVideoSource();
        source.Append([1, 2]);
        source.Append([3, 4, 5]);
        var data = new byte[4];

        source.Read(1, data);

        Assert.That(data, Is.EqualTo(new byte[] { 2, 3, 4, 5 }));
        Assert.That(source.AvailableFrom(2), Is.EqualTo(5));
    }

    [Test]
    public void AppendingAfterTheEndThrows()
    {
        var source = new StreamVideoSource();
        source.End();

        Assert.Throws<InvalidOperationException>(() => source.Append([1]));
    }

    [Test]
    public void ReleasedBytesAreKeptByDefault()
    {
        var source = new StreamVideoSource();
        source.Append(new byte[100]);

        source.ReleaseBefore(80);

        Assert.That(source.BufferedBytes, Is.EqualTo(100));
        Assert.That(source.AvailableFrom(0), Is.EqualTo(100));
    }

    [Test]
    public void ReleasedBytesAreDroppedWhenAsked()
    {
        var source = new StreamVideoSource(true);
        source.Append(new byte[100]);

        source.ReleaseBefore(80);

        Assert.That(source.BufferedBytes, Is.LessThan(100));
        Assert.That(source.Available, Is.EqualTo(100));
        Assert.That(source.AvailableFrom(80), Is.EqualTo(100));
    }
}
