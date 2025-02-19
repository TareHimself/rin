using ManagedBass;
using rin.Framework.Core;

namespace rin.Framework.Audio;

[Module]
public class SAudioModule : IModule
{
    public void Startup(SRuntime runtime)
    {
        Bass.Init();
    }

    public void Shutdown(SRuntime runtime)
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
        return SRuntime.Get().GetModule<SAudioModule>();
    }
}