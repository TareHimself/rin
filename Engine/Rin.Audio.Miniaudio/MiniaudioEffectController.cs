using Rin.Core.Audio;
using Rin.Core.Audio.Effects;

namespace Rin.Audio.Miniaudio;

internal sealed class MiniaudioEffectController<TParams>(ISupportsAudioEffects owner, ulong effectId, IntPtr parametersPtr)
    : IEffectController<TParams> where TParams : unmanaged
{
    private bool _disposed;

    public ulong Id { get; } = effectId;

    public TParams Parameters
    {
        get;
        set
        {
            field = value;
            if (_disposed || parametersPtr == IntPtr.Zero) return;
            unsafe
            {
                ParameterSlots<TParams>.Write(parametersPtr.ToPointer(), value);
            }
        }
    } = new TParams();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        owner.RemoveEffect(Id);
    }
}
