using Rin.Core;

namespace Rin.Audio.Miniaudio.Tests;

public class MiniaudioAudioModuleTests
{
    private RecordingEngine _engine = null!;
    private MiniaudioAudioModule _module = null!;

    [SetUp]
    public void CreateModule()
    {
        _engine = new RecordingEngine();
        _module = new MiniaudioAudioModule(_engine);
    }

    [Test]
    public void Start_InitsTheEngineAndWrapsTheMasterMixer()
    {
        _module.Start(null!);

        var master = (MiniaudioAudioGroup)_module.MasterAudioGroup;
        Assert.Multiple(() =>
        {
            Assert.That(_engine.Calls, Is.EqualTo(new[] { "Init" }));
            Assert.That(master.Id, Is.EqualTo(_engine.MasterMixer));
        });
    }

    [Test]
    public void Start_WhenInitFails_SurfacesTheNativeCode()
    {
        _engine.FailNext(MiniaudioEngine.AudioResult.EngineInitFailed);

        var exception = Assert.Throws<NativeException>(() => _module.Start(null!));

        Assert.That(exception.Code, Is.EqualTo(MiniaudioEngine.AudioResult.EngineInitFailed));
    }

    [Test]
    public void Stop_ShutsTheEngineDown()
    {
        _module.Stop(null!);

        Assert.That(_engine.Calls, Is.EqualTo(new[] { "Shutdown" }));
    }

    [Test]
    public void Volume_IsTheEngineVolume()
    {
        _module.SetVolume(0.25f);

        Assert.Multiple(() =>
        {
            Assert.That(_engine.Volume, Is.EqualTo(0.25f));
            Assert.That(_module.GetVolume(), Is.EqualTo(0.25f));
        });
    }

    [Test]
    public void MakeSample_FromStream_HandsTheWholeStreamToTheEngine()
    {
        byte[] bytes = [1, 2, 3, 4, 5];

        using var sample = _module.MakeSample(new MemoryStream(bytes));

        Assert.Multiple(() =>
        {
            Assert.That(_engine.LoadedBytes, Has.Count.EqualTo(1));
            Assert.That(_engine.LoadedBytes[0], Is.EqualTo(bytes));
            Assert.That(_engine.Calls, Does.Contain("SampleLoad(5 bytes)"));
        });
    }

    [Test]
    public void MakeStream_FromFile_OpensAStreamRatherThanLoadingASample()
    {
        using var sample = _module.MakeStream("song.ogg");

        Assert.That(_engine.Calls, Is.EqualTo(new[] { "StreamOpen(song.ogg)" }));
    }

    [Test]
    public void MakeSample_WhenLoadingFails_SurfacesTheNativeCode()
    {
        _engine.FailNext(MiniaudioEngine.AudioResult.InvalidArgument);

        var exception = Assert.Throws<NativeException>(() => _module.MakeSample("missing.wav"));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Function, Is.EqualTo("SampleLoad"));
            Assert.That(exception.Code, Is.EqualTo(MiniaudioEngine.AudioResult.InvalidArgument));
        });
    }

    [Test]
    public void CreateGroup_IsASubGroupOfTheMaster()
    {
        _module.Start(null!);

        using var group = _module.CreateGroup();

        Assert.That(_engine.Calls, Does.Contain($"MixerCreate({_engine.MasterMixer})"));
    }
}
