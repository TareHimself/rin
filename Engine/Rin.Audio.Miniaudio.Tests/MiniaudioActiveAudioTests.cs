namespace Rin.Audio.Miniaudio.Tests;

public class MiniaudioActiveAudioTests
{
    private const ulong ActiveId = 9;
    private RecordingEngine _engine = null!;
    private MiniaudioActiveAudio _active = null!;

    [SetUp]
    public void CreateActive()
    {
        _engine = new RecordingEngine();
        _active = new MiniaudioActiveAudio(_engine, ActiveId);
    }

    [TearDown]
    public void DisposeSubject()
    {
        _active.Dispose();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Play_PassesTheRestartFlagAndReturnsTheEngineAnswer(bool restart)
    {
        _engine.ActiveResult = restart;

        var applied = _active.Play(restart);

        Assert.Multiple(() =>
        {
            Assert.That(applied, Is.EqualTo(restart));
            Assert.That(_engine.Calls, Is.EqualTo(new[] { $"ActivePlay({ActiveId}, {restart})" }));
        });
    }

    [Test]
    public void Queries_ReadTheEngine()
    {
        _engine.Position = 1.5;
        _engine.Duration = 10;
        _engine.ActiveResult = true;

        Assert.Multiple(() =>
        {
            Assert.That(_active.IsPlaying, Is.True);
            Assert.That(_active.HasEnded, Is.True);
            Assert.That(_active.Position, Is.EqualTo(1.5));
            Assert.That(_active.Duration, Is.EqualTo(10));
        });
    }

    [Test]
    public void Commands_TargetThisHandle()
    {
        _active.Pause();
        _active.SetVolume(0.5f);
        _active.SetPosition(2.5);

        Assert.That(_engine.Calls, Is.EqualTo(new[]
        {
            $"ActivePause({ActiveId})",
            $"ActiveSetVolume({ActiveId}, 0.5)",
            $"ActiveSetPosition({ActiveId}, 2.5)"
        }));
    }

    [Test]
    public void Dispose_Twice_DisposesTheHandleOnce()
    {
        _active.Dispose();
        _active.Dispose();

        Assert.That(_engine.Calls, Is.EqualTo(new[] { $"ActiveDispose({ActiveId})" }));
    }

    [Test]
    public void UseAfterDispose_Throws()
    {
        _active.Dispose();

        Assert.Multiple(() =>
        {
            Assert.Throws<ObjectDisposedException>(() => _ = _active.IsPlaying);
            Assert.Throws<ObjectDisposedException>(() => _active.Play());
            Assert.Throws<ObjectDisposedException>(() => _active.SetPosition(0));
        });
    }
}
