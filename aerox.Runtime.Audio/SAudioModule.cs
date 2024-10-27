using ManagedBass;

namespace aerox.Runtime.Audio;

[NativeRuntimeModule]
public class SAudioModule : RuntimeModule
{
    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);
        Bass.Init();
    }

    public override void Shutdown(SRuntime runtime)
    {
        base.Shutdown(runtime);
        Bass.Free();
    }

/// <summary>
/// Set Volume
/// </summary>
/// <param name="val">Value between 0 and 1</param>
/// <returns></returns>
    public bool SetVolume(float val) => Bass.Configure(Configuration.GlobalStreamVolume, (int)(val * 10000));

    public static SAudioModule Get() => SRuntime.Get().GetModule<SAudioModule>();
}