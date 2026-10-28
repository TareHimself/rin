using Rin.Core;
using Rin.Core.Audio;

namespace Rin.Audio.Miniaudio.Tests;

public class MiniaudioGroupTests
{
    private const ulong MixerId = 7;
    private RecordingEngine _engine = null!;
    private MiniaudioAudioGroup _group = null!;

    [SetUp]
    public void CreateGroup()
    {
        _engine = new RecordingEngine();
        _group = new MiniaudioAudioGroup(_engine, MixerId);
    }

    [TearDown]
    public void DisposeSubject()
    {
        _group.Dispose();
    }

    [Test]
    public void Volume_ReadsAndWritesTheMixer()
    {
        _group.Volume = 0.5f;

        Assert.Multiple(() =>
        {
            Assert.That(_engine.MixerVolumes[MixerId], Is.EqualTo(0.5f));
            Assert.That(_group.Volume, Is.EqualTo(0.5f));
        });
    }

    [Test]
    public void Play_StartsTheSampleOnThisMixer()
    {
        using var sample = new MiniaudioAudioSample(_engine, 42);

        using var channel = _group.Play(sample);

        Assert.That(_engine.Calls, Is.EqualTo(new[] { $"MixerPlay({MixerId}, 42)" }));
    }

    [Test]
    public void Play_RejectsASampleFromAnotherBackend()
    {
        Assert.Throws<ArgumentException>(() => _group.Play(new ForeignSample()));
    }

    [Test]
    public void CreateSubGroup_CreatesAMixerUnderThisOne()
    {
        using var child = (MiniaudioAudioGroup)_group.CreateSubGroup();

        Assert.Multiple(() =>
        {
            Assert.That(_engine.Calls, Is.EqualTo(new[] { $"MixerCreate({MixerId})" }));
            Assert.That(child.Id, Is.Not.EqualTo(MixerId));
        });
    }

    [Test]
    public void CreatePushStream_PassesTheFormat()
    {
        using var stream = _group.CreatePushStream(48000, 2);

        Assert.That(_engine.Calls, Is.EqualTo(new[] { $"MixerCreatePushStream({MixerId}, 48000, 2)" }));
    }

    [Test]
    public void AddEffect_WhenTheMixerRejectsIt_SurfacesTheNativeCode()
    {
        _engine.FailNext(MiniaudioEngine.AudioResult.MiniaudioError);

        var exception = Assert.Throws<NativeException>(() => _group.AddEffect(new FakeEffectDescriptor()));

        Assert.That(exception.Code, Is.EqualTo(MiniaudioEngine.AudioResult.MiniaudioError));
    }

    [Test]
    public void DisposingTheController_RemovesTheEffectOnce()
    {
        var controller = _group.AddEffect(new FakeEffectDescriptor());

        controller.Dispose();
        controller.Dispose();
        _group.RemoveEffect(controller.Id);

        Assert.That(_engine.Calls.Count(call => call.StartsWith("MixerRemoveEffect")), Is.EqualTo(1));
    }

    [Test]
    public void RemoveEffect_ReleasesTheDescriptorBuffers()
    {
        var descriptor = new FakeEffectDescriptor();
        var controller = _group.AddEffect(descriptor);

        _group.RemoveEffect(controller.Id);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.ReleasedParameters, Is.EqualTo(1));
            Assert.That(descriptor.ReleasedStates, Is.EqualTo(1));
        });
    }

    [Test]
    public void Controller_WritesParametersIntoTheSharedBuffer()
    {
        var controller = _group.AddEffect(new FakeEffectDescriptor());

        controller.Parameters = new FakeEffectParameters { Gain = 2f };

        Assert.That(controller.Parameters.Gain, Is.EqualTo(2f));
        _group.RemoveEffect(controller.Id);
    }

    [Test]
    public void Dispose_RemovesEffectsBeforeDisposingTheMixer()
    {
        var first = _group.AddEffect(new FakeEffectDescriptor());
        var second = _group.AddEffect(new FakeEffectDescriptor());
        _engine.Calls.Clear();

        _group.Dispose();

        Assert.That(_engine.Calls, Is.EqualTo(new[]
        {
            $"MixerRemoveEffect({MixerId}, {first.Id})",
            $"MixerRemoveEffect({MixerId}, {second.Id})",
            $"MixerDispose({MixerId})"
        }));
    }

    [Test]
    public void Dispose_Twice_DisposesTheMixerOnce()
    {
        _group.Dispose();
        _group.Dispose();

        Assert.That(_engine.Calls, Is.EqualTo(new[] { $"MixerDispose({MixerId})" }));
    }

    [Test]
    public void Dispose_OnTheMasterGroup_DoesNothing()
    {
        var master = new MiniaudioAudioGroup(_engine, _engine.MasterMixer, isMaster: true);

        master.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(_engine.Calls, Is.Empty);
            Assert.DoesNotThrow(() => master.Volume = 1f);
        });
    }

    [Test]
    public void UseAfterDispose_Throws()
    {
        _group.Dispose();

        Assert.Multiple(() =>
        {
            Assert.Throws<ObjectDisposedException>(() => _ = _group.Volume);
            Assert.Throws<ObjectDisposedException>(() => _group.CreateSubGroup());
            Assert.Throws<ObjectDisposedException>(() => _group.AddEffect(new FakeEffectDescriptor()));
        });
    }

    private sealed class ForeignSample : IAudioSample
    {
        public IActiveAudio MakeActive() => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
