using Rin.Core;
using Rin.Core.Audio;

namespace Rin.Audio.Null;

public sealed class NullAudioModule : IAudioModule
{
    private float _volume = 1.0f;

    public IAudioGroup MasterAudioGroup { get; } = new NullAudioGroup();

    public void Start(IApplication app)
    {
    }

    public void Stop(IApplication app)
    {
    }

    public void Update(float deltaTime)
    {
    }

    public float GetVolume()
    {
        return _volume;
    }

    public void SetVolume(float volume)
    {
        _volume = volume;
    }

    public IAudioSample MakeSample(string filePath)
    {
        return new NullAudioSample();
    }

    public IAudioSample MakeSample(Stream fileStream)
    {
        return new NullAudioSample();
    }

    public IAudioSample MakeStream(string filePath)
    {
        return new NullAudioSample();
    }

    public IAudioSample MakeStream(Stream fileStream)
    {
        return new NullAudioSample();
    }

    public IAudioGroup CreateGroup()
    {
        return new NullAudioGroup();
    }
}
