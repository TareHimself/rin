using System.Runtime.CompilerServices;

namespace Rin.Core.Audio.Effects;

public class AudioEffectInstance<TParams> : IAudioEffectInstance where TParams : unmanaged 
{
    public IntPtr MethodPointer { get; set; }
    public IntPtr ParameterPointer { get; set; }
    public IntPtr StatePointer { get; set; }

    public void UpdateParameters(TParams parameters)
    {
        if(ParameterPointer == IntPtr.Zero) return;
        unsafe
        {
            ref var asRef = ref Unsafe.AsRef<TParams>(ParameterPointer.ToPointer());
            asRef = parameters;
        }
    }

    private void ReleaseUnmanagedResources()
    {
        
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~AudioEffectInstance()
    {
        ReleaseUnmanagedResources();
    }
}