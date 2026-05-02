using ManagedBass;

namespace Rin.Framework.Audio.Bass;

public class BassAudioModule : IAudioModule
{
    public void Start(IApplication app)
    {
        ManagedBass.Bass.Init();
    }

    public void Stop(IApplication app)
    {
        ManagedBass.Bass.Free();
    }

    public void Update(float deltaTime)
    {
    }

    public float GetVolume()
    {
        return ManagedBass.Bass.GetConfig(Configuration.GlobalStreamVolume) / 10000.0f;
    }

    public void SetVolume(float volume)
    {
        ManagedBass.Bass.Configure(Configuration.GlobalStreamVolume, (int)(volume * 10000));
    }

    public IAudioSample MakeSample(string filePath)
    {
        throw new NotImplementedException();
    }

    public IAudioSample MakeSample(Stream fileStream)
    {
        throw new NotImplementedException();
    }

    public IAudioSample MakeStream(string filePath)
    {
        return new BassFileStreamSample(filePath);
    }

    public IAudioSample MakeStream(Stream fileStream)
    {
        throw new NotImplementedException();
    }

    public IPushStream MakePushStream(int frequency, int channels)
    {
        var bassStream = ManagedBass.Bass.CreateStream(frequency, channels, BassFlags.Float, StreamProcedureType.Push);
        return new BassPushStream(bassStream);
    }

    public IAudioScene CreateScene()
    {
        throw new NotImplementedException();
    }
}