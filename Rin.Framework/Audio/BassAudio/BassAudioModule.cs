using ManagedBass;

namespace Rin.Framework.Audio.BassAudio;

public class BassAudioModule : IAudioModule
{
    public void Start(IApplication app)
    {
        Bass.Init();
    }

    public void Stop(IApplication app)
    {
        Bass.Free();
    }

    public void Update(float deltaTime)
    {
        
    }

    public void SetVolume(float volume)
    {
        Bass.Configure(Configuration.GlobalStreamVolume, (int)(volume * 10000));
    }

    public IStream CreateStream(Stream source)
    {
        throw new NotImplementedException();
    }

    public IPushStream CreatePushStream(int frequency, int channels)
    {
        var bassStream = Bass.CreateStream(frequency, channels, BassFlags.Float, StreamProcedureType.Push);
        return new BassPushStream(bassStream);
    }
}