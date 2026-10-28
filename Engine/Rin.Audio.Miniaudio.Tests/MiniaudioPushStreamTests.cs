namespace Rin.Audio.Miniaudio.Tests;

public class MiniaudioPushStreamTests
{
    private const ulong StreamId = 11;
    private RecordingEngine _engine = null!;
    private MiniaudioPushStream _stream = null!;

    [SetUp]
    public void CreateStream()
    {
        _engine = new RecordingEngine();
        _stream = new MiniaudioPushStream(_engine, StreamId);
    }

    [TearDown]
    public void DisposeSubject()
    {
        _stream.Dispose();
    }

    [Test]
    public void Push_HandsTheBytesToThisStream()
    {
        byte[] bytes = [1, 2, 3, 4];

        _stream.Push(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(_engine.Calls, Is.EqualTo(new[] { $"PushStreamPush({StreamId}, 4 bytes)" }));
            Assert.That(_engine.Pushed[0], Is.EqualTo(bytes));
        });
    }

    [Test]
    public void Push_AfterDispose_Throws()
    {
        _stream.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _stream.Push(new byte[4]));
    }
}
