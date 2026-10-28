using System.Runtime.InteropServices;
using Rin.Core;
// ReSharper disable InconsistentNaming

namespace Rin.Audio.Miniaudio;

internal sealed partial class MiniaudioEngine : IMiniaudioEngine
{
    private const string Lib = "Rin.Audio.Miniaudio.Native";

    // Mirrors AudioResult in native/Rin.Audio.Miniaudio.Native/src/result.hpp. Keep the two in sync.
    internal enum AudioResult
    {
        Ok = 0,
        InvalidArgument,
        InvalidHandle,
        EngineInitFailed,
        ListenerLimitReached,
        MiniaudioError,
        OutOfMemory,
        PlatformError
    }

    public float Volume
    {
        get => audioGetVolume();
        set => audioSetVolume(value);
    }

    public ulong MasterMixer => audioGetMasterMixerId();

    public void Init() => Check(audioInit(), nameof(audioInit));

    public void Shutdown() => audioShutdown();

    public ulong SampleLoad(string filePath)
    {
        Check(audioMakeSampleFromFile(filePath, out var id), nameof(audioMakeSampleFromFile));
        return id;
    }

    public unsafe ulong SampleLoad(ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
        {
            Check(audioMakeSampleFromMemory(ptr, (nuint)data.Length, out var id), nameof(audioMakeSampleFromMemory));
            return id;
        }
    }

    public ulong StreamOpen(string filePath)
    {
        Check(audioMakeStreamFromFile(filePath, out var id), nameof(audioMakeStreamFromFile));
        return id;
    }

    public unsafe ulong StreamOpen(ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
        {
            Check(audioMakeStreamFromMemory(ptr, (nuint)data.Length, out var id), nameof(audioMakeStreamFromMemory));
            return id;
        }
    }

    public ulong SampleMakeActive(ulong sample)
    {
        Check(audioSampleMakeActive(sample, out var id), nameof(audioSampleMakeActive));
        return id;
    }

    public void SampleDispose(ulong sample) => audioSampleDispose(sample);

    public ulong MixerCreate(ulong parent)
    {
        Check(audioMixerCreate(parent, out var id), nameof(audioMixerCreate));
        return id;
    }

    public void MixerDispose(ulong mixer) => audioMixerDispose(mixer);

    public float MixerGetVolume(ulong mixer) => audioMixerGetVolume(mixer);

    public void MixerSetVolume(ulong mixer, float volume) => audioMixerSetVolume(mixer, volume);

    public ulong MixerPlay(ulong mixer, ulong sample)
    {
        Check(audioMixerPlay(mixer, sample, out var id), nameof(audioMixerPlay));
        return id;
    }

    public ulong MixerCreatePushStream(ulong mixer, int sampleRate, int channels)
    {
        Check(audioMixerCreatePushStream(mixer, sampleRate, channels, out var id), nameof(audioMixerCreatePushStream));
        return id;
    }

    public void MixerAddEffect(ulong mixer, in NativeEffectManager.NativeAudioEffect effect) =>
        Check(audioMixerAddEffect(mixer, effect), nameof(audioMixerAddEffect));

    public void MixerRemoveEffect(ulong mixer, ulong effect) => audioMixerRemoveEffect(mixer, effect);

    public bool ActiveIsPlaying(ulong active) => audioActiveIsPlaying(active) != 0;

    public bool ActiveHasEnded(ulong active) => audioActiveHasEnded(active) != 0;

    public double ActiveGetPosition(ulong active) => audioActiveGetPosition(active);

    public double ActiveGetDuration(ulong active) => audioActiveGetDuration(active);

    public bool ActivePlay(ulong active, bool restart) => audioActivePlay(active, restart ? 1 : 0) != 0;

    public bool ActivePause(ulong active) => audioActivePause(active) != 0;

    public bool ActiveSetVolume(ulong active, float volume) => audioActiveSetVolume(active, volume) != 0;

    public bool ActiveSetPosition(ulong active, double seconds) => audioActiveSetPosition(active, seconds) != 0;

    public void ActiveDispose(ulong active) => audioActiveDispose(active);

    public unsafe nuint PushStreamPush(ulong stream, ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
        {
            return audioPushStreamPush(stream, ptr, (nuint)data.Length);
        }
    }

    private static void Check(AudioResult result, string function)
    {
        if (result != AudioResult.Ok) throw new NativeException(Lib, function, result);
    }

    [LibraryImport(Lib)] private static partial AudioResult audioInit();
    [LibraryImport(Lib)] private static partial void audioShutdown();
    [LibraryImport(Lib)] private static partial float audioGetVolume();
    [LibraryImport(Lib)] private static partial void audioSetVolume(float volume);
    [LibraryImport(Lib)] private static partial ulong audioGetMasterMixerId();

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    private static partial AudioResult audioMakeSampleFromFile(string filePath, out ulong outSampleId);

    [LibraryImport(Lib)]
    private static unsafe partial AudioResult audioMakeSampleFromMemory(byte* data, nuint size, out ulong outSampleId);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    private static partial AudioResult audioMakeStreamFromFile(string filePath, out ulong outSampleId);

    [LibraryImport(Lib)]
    private static unsafe partial AudioResult audioMakeStreamFromMemory(byte* data, nuint size, out ulong outSampleId);

    [LibraryImport(Lib)] private static partial AudioResult audioSampleMakeActive(ulong sampleId, out ulong outActiveId);
    [LibraryImport(Lib)] private static partial void audioSampleDispose(ulong sampleId);

    [LibraryImport(Lib)] private static partial AudioResult audioMixerCreate(ulong parentId, out ulong outMixerId);
    [LibraryImport(Lib)] private static partial void audioMixerDispose(ulong mixerId);
    [LibraryImport(Lib)] private static partial float audioMixerGetVolume(ulong mixerId);
    [LibraryImport(Lib)] private static partial void audioMixerSetVolume(ulong mixerId, float volume);
    [LibraryImport(Lib)] private static partial AudioResult audioMixerPlay(ulong mixerId, ulong sampleId, out ulong outActiveId);
    [LibraryImport(Lib)] private static partial AudioResult audioMixerCreatePushStream(ulong mixerId, int sampleRate, int channels, out ulong outActiveId);
    [LibraryImport(Lib)] private static partial AudioResult audioMixerAddEffect(ulong mixerId, NativeEffectManager.NativeAudioEffect effect);
    [LibraryImport(Lib)] private static partial void audioMixerRemoveEffect(ulong mixerId, ulong effectId);

    [LibraryImport(Lib)] private static partial int audioActiveIsPlaying(ulong id);
    [LibraryImport(Lib)] private static partial int audioActiveHasEnded(ulong id);
    [LibraryImport(Lib)] private static partial double audioActiveGetPosition(ulong id);
    [LibraryImport(Lib)] private static partial double audioActiveGetDuration(ulong id);
    [LibraryImport(Lib)] private static partial int audioActivePlay(ulong id, int restart);
    [LibraryImport(Lib)] private static partial int audioActivePause(ulong id);
    [LibraryImport(Lib)] private static partial int audioActiveSetVolume(ulong id, float volume);
    [LibraryImport(Lib)] private static partial int audioActiveSetPosition(ulong id, double positionSeconds);
    [LibraryImport(Lib)] private static partial void audioActiveDispose(ulong id);

    [LibraryImport(Lib)]
    private static unsafe partial nuint audioPushStreamPush(ulong id, byte* data, nuint size);

    // [LibraryImport(Lib)] internal static partial ulong audioCreateScene(ulong parentId);
    //
    // [LibraryImport(Lib)]
    // internal static partial void audioSceneSetListenerPose(ulong sceneId,
    //     Vector3 position, Vector3 forward, Vector3 right);
    //
    // [LibraryImport(Lib)]
    // internal static partial ulong audioScenePlayAtLocation(ulong sceneId, ulong sampleId, Vector3 location);
    //
    // [LibraryImport(Lib)] internal static partial ulong audioScenePlay2d(ulong sceneId, ulong sampleId);
    // [LibraryImport(Lib)] internal static partial float audioSceneGetVolume(ulong sceneId);
    // [LibraryImport(Lib)] internal static partial void  audioSceneSetVolume(ulong sceneId, float volume);
    // [LibraryImport(Lib)] internal static partial void  audioSceneDispose(ulong sceneId);
}
