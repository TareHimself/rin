// using System.Numerics;
// using Rin.Core.Audio;
//
// namespace Rin.Audio.Miniaudio;
//
// public class MiniaudioDirectionalAudioMixer : IDirectionalAudioGroup
// {
//     private readonly ulong _id;
//     private bool _disposed;
//
//     internal MiniaudioDirectionalAudioMixer(ulong id) { _id = id; }
//
//     internal ulong Id => _id;
//
//     public float Volume
//     {
//         get { ThrowIfDisposed(); return Native.audioSceneGetVolume(_id); }
//         set { ThrowIfDisposed(); Native.audioSceneSetVolume(_id, value); }
//     }
//
//     public IChannel Play(IAudioSample sample)
//     {
//         ThrowIfDisposed();
//         return new MiniaudioActiveAudio(Native.audioScenePlay2d(_id, SampleId(sample)));
//     }
//
//     public IChannel PlayAtPosition(IAudioSample sample, Vector3 position)
//     {
//         ThrowIfDisposed();
//         return new MiniaudioActiveAudio(Native.audioScenePlayAtLocation(_id, SampleId(sample), position));
//     }
//
//     public IPushStream CreatePushStream(int frequency, int channels)
//     {
//         ThrowIfDisposed();
//         return new MiniaudioPushStream(Native.audioMixerCreatePushStream(_id, frequency, channels));
//     }
//
//     public IAudioGroup CreateSubGroup()
//     {
//         ThrowIfDisposed();
//         return new MiniaudioAudioGroup(Native.audioMixerCreate(_id));
//     }
//
//     public IDirectionalAudioGroup CreateChildDirectionalBus()
//     {
//         ThrowIfDisposed();
//         return new MiniaudioDirectionalAudioMixer(Native.audioCreateScene(_id));
//     }
//
//     public void SetListenerPose(Vector3 position, Vector3 forward, Vector3 right)
//     {
//         ThrowIfDisposed();
//         Native.audioSceneSetListenerPose(_id, position, forward, right);
//     }
//
//     public void Dispose()
//     {
//         if (_disposed) return;
//         _disposed = true;
//         GC.SuppressFinalize(this);
//         Native.audioSceneDispose(_id);
//     }
//
//     private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
//
//     private static ulong SampleId(IAudioSample s)
//     {
//         if (s is not MiniaudioAudioSample m)
//             throw new ArgumentException("must be MiniaudioAudioSample", nameof(s));
//         return m.NativeId;
//     }
// }
