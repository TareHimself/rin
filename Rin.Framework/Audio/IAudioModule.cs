namespace Rin.Framework.Audio;

public interface IAudioModule : IModule, IUpdatable
{
    public void SetVolume(float volume);
    public IStream CreateStream(Stream source);
    public IPushStream CreatePushStream(int frequency, int channels);
    
    public static IAudioModule Get() => SFramework.Provider.Get<IAudioModule>();
}