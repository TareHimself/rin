namespace Rin.Core.Audio.Effects;

/// <summary>
/// Native-interop surface for an audio effect: the process method's function pointer,
/// plus allocation/release for its unmanaged parameter and state buffers. This is entirely generated -
/// see <see cref="global::Rin.Core.Audio.Effects.AudioEffectAttribute"/> - you should never implement it by hand.
/// </summary>
public interface IAudioEffectDescriptor
{
    IntPtr GetProcessMethodPtr();
    IntPtr CreateState();
    IntPtr CreateParameters();
    void ReleaseState(IntPtr state);
    void ReleaseParameters(IntPtr parameters);
}

/// <summary>Strongly-typed by the effect's parameter struct so <see cref="ISupportsAudioEffects.AddEffect{TParams}"/> can infer <c>TParams</c>.</summary>
public interface IAudioEffectDescriptor<TParams> : IAudioEffectDescriptor where TParams : unmanaged
{
}
