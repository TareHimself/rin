using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Core;
// ReSharper disable InconsistentNaming

namespace Rin.Audio.Miniaudio;

internal static partial class Native
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

    private static void Check(AudioResult result, string function)
    {
        if (result != AudioResult.Ok) throw new NativeException(Lib, function, result);
    }

    internal static void Init() => Check(audioInit(), nameof(audioInit));

    internal static ulong MakeSampleFromFile(string filePath)
    {
        Check(audioMakeSampleFromFile(filePath, out var id), nameof(audioMakeSampleFromFile));
        return id;
    }

    internal static unsafe ulong MakeSampleFromMemory(byte* data, nuint size)
    {
        Check(audioMakeSampleFromMemory(data, size, out var id), nameof(audioMakeSampleFromMemory));
        return id;
    }

    internal static ulong MakeStreamFromFile(string filePath)
    {
        Check(audioMakeStreamFromFile(filePath, out var id), nameof(audioMakeStreamFromFile));
        return id;
    }

    internal static unsafe ulong MakeStreamFromMemory(byte* data, nuint size)
    {
        Check(audioMakeStreamFromMemory(data, size, out var id), nameof(audioMakeStreamFromMemory));
        return id;
    }

    internal static ulong SampleMakeActive(ulong sampleId)
    {
        Check(audioSampleMakeActive(sampleId, out var id), nameof(audioSampleMakeActive));
        return id;
    }

    internal static ulong MixerCreate(ulong parentId)
    {
        Check(audioMixerCreate(parentId, out var id), nameof(audioMixerCreate));
        return id;
    }

    internal static ulong MixerPlay(ulong mixerId, ulong sampleId)
    {
        Check(audioMixerPlay(mixerId, sampleId, out var id), nameof(audioMixerPlay));
        return id;
    }

    internal static ulong MixerCreatePushStream(ulong mixerId, int sampleRate, int channels)
    {
        Check(audioMixerCreatePushStream(mixerId, sampleRate, channels, out var id), nameof(audioMixerCreatePushStream));
        return id;
    }

    internal static void MixerAddEffect(ulong mixerId, NativeEffectManager.NativeAudioEffect effect) =>
        Check(audioMixerAddEffect(mixerId, effect), nameof(audioMixerAddEffect));

    [LibraryImport(Lib)] private static partial AudioResult audioInit();
    [LibraryImport(Lib)] internal static partial void  audioShutdown();
    [LibraryImport(Lib)] internal static partial float audioGetVolume();
    [LibraryImport(Lib)] internal static partial void  audioSetVolume(float volume);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    private static partial AudioResult audioMakeSampleFromFile(string filePath, out ulong outSampleId);

    [LibraryImport(Lib)]
    private static unsafe partial AudioResult audioMakeSampleFromMemory(byte* data, nuint size, out ulong outSampleId);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    private static partial AudioResult audioMakeStreamFromFile(string filePath, out ulong outSampleId);

    [LibraryImport(Lib)]
    private static unsafe partial AudioResult audioMakeStreamFromMemory(byte* data, nuint size, out ulong outSampleId);

    [LibraryImport(Lib)] internal static partial void  audioSampleDispose(ulong sampleId);
    [LibraryImport(Lib)] private static partial AudioResult audioSampleMakeActive(ulong sampleId, out ulong outActiveId);

    [LibraryImport(Lib)] internal static partial int    audioActiveIsPlaying(ulong id);
    [LibraryImport(Lib)] internal static partial int    audioActiveHasEnded(ulong id);
    [LibraryImport(Lib)] internal static partial double audioActiveGetPosition(ulong id);
    [LibraryImport(Lib)] internal static partial double audioActiveGetDuration(ulong id);
    [LibraryImport(Lib)] internal static partial int    audioActivePlay(ulong id, int restart);
    [LibraryImport(Lib)] internal static partial int    audioActivePause(ulong id);
    [LibraryImport(Lib)] internal static partial int    audioActiveSetVolume(ulong id, float volume);
    [LibraryImport(Lib)] internal static partial int    audioActiveSetPosition(ulong id, double positionSeconds);
    [LibraryImport(Lib)] internal static partial void   audioActiveDispose(ulong id);

    [LibraryImport(Lib)]
    internal static unsafe partial nuint audioPushStreamPush(ulong id, byte* data, nuint size);

    [LibraryImport(Lib)] private static partial AudioResult audioMixerAddEffect(ulong mixerId, NativeEffectManager.NativeAudioEffect effect);
    [LibraryImport(Lib)] internal static partial void audioMixerRemoveEffect(ulong mixerId, ulong effectId);

    [LibraryImport(Lib)] private static partial AudioResult audioMixerCreate(ulong parentId, out ulong outMixerId);
    [LibraryImport(Lib)] internal static partial void  audioMixerDispose(ulong mixerId);
    [LibraryImport(Lib)] internal static partial float audioMixerGetVolume(ulong mixerId);
    [LibraryImport(Lib)] internal static partial void  audioMixerSetVolume(ulong mixerId, float volume);
    [LibraryImport(Lib)] private static partial AudioResult audioMixerPlay(ulong mixerId, ulong sampleId, out ulong outActiveId);
    [LibraryImport(Lib)] private static partial AudioResult audioMixerCreatePushStream(ulong mixerId, int sampleRate, int channels, out ulong outActiveId);
    [LibraryImport(Lib)] internal static partial ulong audioGetMasterMixerId();

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
