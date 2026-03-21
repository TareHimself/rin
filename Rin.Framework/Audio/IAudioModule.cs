namespace Rin.Framework.Audio;

public interface IAudioModule : IModule, IUpdatable
{
    public float GetVolume();
    public void SetVolume(float volume);
    
    public IAudioSample MakeSample(string filePath);
    public IAudioSample MakeSample(Stream fileStream);
    public IAudioSample MakeStream(string filePath);
    public IAudioSample MakeStream(Stream fileStream);
    
    public IPushStream MakePushStream(int frequency, int channels);
    
    public static IAudioModule Get() => SFramework.Provider.Get<IAudioModule>();
    public IAudioScene CreateScene();
}