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


    public bool SetVolume(int val) => Bass.Configure(Configuration.GlobalMusicVolume, val);

    public static SAudioModule Get() => SRuntime.Get().GetModule<SAudioModule>();
}