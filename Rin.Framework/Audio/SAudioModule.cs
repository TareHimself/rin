using ManagedBass;
using Rin.Framework.Audio.BassAudio;

namespace Rin.Framework.Audio;

[Module]
public class SAudioModule : IModule
{
    public void Start(SApplication application)
    {
        Bass.Init();
    }

    public void Stop(SApplication application)
    {
        Bass.Free();
    }

    /// <summary>
    ///     Set Volume
    /// </summary>
    /// <param name="val">Value between 0 and 1</param>
    /// <returns></returns>
    public bool SetVolume(float val)
    {
        return Bass.Configure(Configuration.GlobalStreamVolume, (int)(val * 10000));
    }

    public static SAudioModule Get()
    {
        return SApplication.Get().GetModule<SAudioModule>();
    }

    public IPushStream CreatePushStream(int frequency, int channels)
    {
        var bassStream = Bass.CreateStream(frequency, channels, BassFlags.Float, StreamProcedureType.Push);
        return new BassPushStream(bassStream);
    }
}