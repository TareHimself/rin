using ManagedBass;
using Rin.Engine.Core;

namespace Rin.Engine.Audio;

[Module]
public class SAudioModule : IModule
{
    public void Start(SEngine engine)
    {
        Bass.Init();
    }

    public void Stop(SEngine engine)
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
        return SEngine.Get().GetModule<SAudioModule>();
    }
}