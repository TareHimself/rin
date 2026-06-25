namespace Rin.Core.Audio.Effects;

public interface IAudioEffectController<TParams> where TParams : unmanaged
{
    public void UpdateParameters(in TParams parameters);
}