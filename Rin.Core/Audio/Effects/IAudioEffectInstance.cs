namespace Rin.Core.Audio.Effects;

public interface IAudioEffectInstance : IDisposable
{
    public IntPtr MethodPointer { get; }
    public IntPtr ParameterPointer { get; }
    public IntPtr StatePointer { get; }
}