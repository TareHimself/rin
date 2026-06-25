using Rin.Core.Audio;
using Rin.Core.Audio.Effects;

namespace Rin.Audio.Miniaudio;

internal class MiniaudioEffectController<TParams> : IEffectController<TParams> where TParams : unmanaged
{
    private readonly ulong _mixerId;
    private readonly IntPtr _parametersPtr;
    private bool _disposed;
    private readonly ISupportsAudioEffects _owner;
    
    public ulong Id { get; }

    internal MiniaudioEffectController(ISupportsAudioEffects owner,ulong effectId, ulong mixerId, IntPtr parametersPtr)
    {
        _owner = owner;
        Id = effectId;
        _mixerId = mixerId;
        _parametersPtr = parametersPtr;
        Parameters = new TParams();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _owner.RemoveEffect(Id);
        Native.audioMixerRemoveEffect(_mixerId, Id);
    }

    public TParams Parameters
    {
        get;
        set
        {
            field = value;
            unsafe
            {
                if (_disposed) return;
                if (_parametersPtr == IntPtr.Zero) return;

                var paramsPtr = (TParams*)_parametersPtr;
                *paramsPtr = value;
            }
        }
    }
}