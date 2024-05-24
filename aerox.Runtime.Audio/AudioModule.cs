using ManagedBass;

namespace aerox.Runtime.Audio;

[NativeRuntimeModule]
public class AudioModule : RuntimeModule
{
    public override void Startup(Runtime runtime)
    {
        base.Startup(runtime);
        Bass.Init();
    }

    public override void Shutdown(Runtime runtime)
    {
        base.Shutdown(runtime);
        Bass.Free();
    }
}