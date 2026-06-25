namespace Rin.Core.Audio.Effects;

public interface IAudioEffectDescriptor
{
    IntPtr GetProcessMethodPtr();
    IntPtr CreateState();
    IntPtr CreateParameters();
    void ReleaseState(IntPtr state);
    void ReleaseParameters(IntPtr parameters);
}

public interface IAudioEffectDescriptor<TParams> : IAudioEffectDescriptor where TParams : unmanaged
{
}
