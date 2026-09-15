namespace Rin.Core.Audio;

public interface IAudioModule : IModule, IUpdatable, IProviderResolvable<IAudioModule>
{
    IAudioGroup MasterAudioGroup { get; }

    public float GetVolume();
    public void SetVolume(float volume);

    public IAudioSample MakeSample(string filePath);
    public IAudioSample MakeSample(Stream fileStream);
    public IAudioSample MakeStream(string filePath);
    public IAudioSample MakeStream(Stream fileStream);
    
    IAudioGroup CreateGroup();
}