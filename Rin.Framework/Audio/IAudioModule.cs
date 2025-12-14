namespace Rin.Framework.Audio;

public interface IAudioModule : IModule, IUpdatable
{
    public float GetVolume();
    public void SetVolume(float volume);
    public ISample CreateSample(string filePath);
    public IStream CreateStream(string filePath);
    public IPushStream CreatePushStream(int frequency, int channels);
    
    public static IAudioModule Get() => SFramework.Provider.Get<IAudioModule>();
}