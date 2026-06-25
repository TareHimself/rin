using System.Runtime.InteropServices;
using Rin.Core.Audio.Effects;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Rin.Audio.Miniaudio;

internal static class NativeEffectManager
{
    [StructLayout(LayoutKind.Sequential),NoReorder]
    public struct NativeAudioEffect
    {
        public ulong EffectId;
        public IntPtr DescriptorHandle;
        public IntPtr Parameters;
        public IntPtr State;
        public unsafe delegate* unmanaged[Cdecl]<float*, float*, int, AudioEffectContext*, void*, void*, void> ProcessCallback;
        public unsafe delegate* unmanaged[Cdecl]<IntPtr, IntPtr,IntPtr, void> ReleaseCallback;

        public NativeAudioEffect(ulong effectId,IAudioEffectDescriptor descriptor)
        {
            EffectId = effectId;
            DescriptorHandle = GCHandle.ToIntPtr(GCHandle.Alloc(descriptor,GCHandleType.Normal));
            Parameters = descriptor.CreateParameters();
            State = descriptor.CreateState();
            unsafe
            {
                ProcessCallback = (delegate* unmanaged[Cdecl]<float*, float*, int, AudioEffectContext*, void*, void*, void>)descriptor.GetProcessMethodPtr();
                ReleaseCallback = &HandleReleaseEffect;
            }
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void HandleReleaseEffect(IntPtr handle,IntPtr parameters,IntPtr state)
    {
        var gcHandle = GCHandle.FromIntPtr(handle);
        if (gcHandle.Target is not IAudioEffectDescriptor descriptor) return;
        descriptor.ReleaseParameters(parameters);
        descriptor.ReleaseState(state);
        gcHandle.Free();
    }
}