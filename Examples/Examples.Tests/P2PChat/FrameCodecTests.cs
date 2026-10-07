using Examples.P2PChat.Net;

namespace Examples.Tests.P2PChat;

public class FrameCodecTests
{
    [Test]
    public async Task FrameSurvivesARoundTrip()
    {
        var stream = new MemoryStream();
        var frame = new Frame(FrameKind.Chat, "ann", "héllo wörld");

        await FrameCodec.WriteAsync(stream, frame, CancellationToken.None);
        stream.Position = 0;

        Assert.That(await FrameCodec.ReadAsync(stream, CancellationToken.None), Is.EqualTo(frame));
    }

    [Test]
    public async Task ReadingAClosedStreamReturnsNull()
    {
        var stream = new MemoryStream();

        Assert.That(await FrameCodec.ReadAsync(stream, CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task TwoFramesInARowAreReadSeparately()
    {
        var stream = new MemoryStream();
        await FrameCodec.WriteAsync(stream, new Frame(FrameKind.Chat, "a", "one"), CancellationToken.None);
        await FrameCodec.WriteAsync(stream, new Frame(FrameKind.Chat, "a", "two"), CancellationToken.None);
        stream.Position = 0;

        Assert.That((await FrameCodec.ReadAsync(stream, CancellationToken.None))!.Text, Is.EqualTo("one"));
        Assert.That((await FrameCodec.ReadAsync(stream, CancellationToken.None))!.Text, Is.EqualTo("two"));
    }

    [Test]
    public void OversizedLengthIsRejected()
    {
        var stream = new MemoryStream([0x7f, 0xff, 0xff, 0xff]);

        Assert.ThrowsAsync<InvalidDataException>(() => FrameCodec.ReadAsync(stream, CancellationToken.None));
    }
}
