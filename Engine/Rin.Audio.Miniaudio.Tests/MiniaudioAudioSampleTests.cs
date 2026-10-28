namespace Rin.Audio.Miniaudio.Tests;

public class MiniaudioAudioSampleTests
{
    private const ulong SampleId = 3;
    private RecordingEngine _engine = null!;
    private MiniaudioAudioSample _sample = null!;

    [SetUp]
    public void CreateSample()
    {
        _engine = new RecordingEngine();
        _sample = new MiniaudioAudioSample(_engine, SampleId);
    }

    [TearDown]
    public void DisposeSubject()
    {
        _sample.Dispose();
    }

    [Test]
    public void MakeActive_CreatesAnActiveFromThisSample()
    {
        using var active = _sample.MakeActive();

        Assert.That(_engine.Calls, Is.EqualTo(new[] { $"SampleMakeActive({SampleId})" }));
    }

    [Test]
    public void Dispose_Twice_DisposesTheSampleOnce()
    {
        _sample.Dispose();
        _sample.Dispose();

        Assert.That(_engine.Calls, Is.EqualTo(new[] { $"SampleDispose({SampleId})" }));
    }

    [Test]
    public void MakeActive_AfterDispose_Throws()
    {
        _sample.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _sample.MakeActive());
    }
}
