using System.Numerics;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming

namespace Rin.Audio.Miniaudio;

internal static partial class Native
{
    private const string Lib = "Rin.Audio.Miniaudio.Native";

    [LibraryImport(Lib)] internal static partial int   audioInit();
    [LibraryImport(Lib)] internal static partial void  audioShutdown();
    [LibraryImport(Lib)] internal static partial float audioGetVolume();
    [LibraryImport(Lib)] internal static partial void  audioSetVolume(float volume);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial ulong audioMakeSampleFromFile(string filePath);

    [LibraryImport(Lib)]
    internal static unsafe partial ulong audioMakeSampleFromMemory(byte* data, nuint size);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial ulong audioMakeStreamFromFile(string filePath);

    [LibraryImport(Lib)]
    internal static unsafe partial ulong audioMakeStreamFromMemory(byte* data, nuint size);

    [LibraryImport(Lib)] internal static partial void  audioSampleDispose(ulong sampleId);
    [LibraryImport(Lib)] internal static partial ulong audioSampleMakeActive(ulong sampleId);

    [LibraryImport(Lib)] internal static partial int    audioActiveIsPlaying(ulong id);
    [LibraryImport(Lib)] internal static partial double audioActiveGetPosition(ulong id);
    [LibraryImport(Lib)] internal static partial double audioActiveGetDuration(ulong id);
    [LibraryImport(Lib)] internal static partial int    audioActivePlay(ulong id, int restart);
    [LibraryImport(Lib)] internal static partial int    audioActivePause(ulong id);
    [LibraryImport(Lib)] internal static partial int    audioActiveSetVolume(ulong id, float volume);
    [LibraryImport(Lib)] internal static partial int    audioActiveSetPosition(ulong id, double positionSeconds);
    [LibraryImport(Lib)] internal static partial void   audioActiveDispose(ulong id);

    [LibraryImport(Lib)]
    internal static unsafe partial nuint audioPushStreamPush(ulong id, byte* data, nuint size);

    [LibraryImport(Lib)] internal static partial int  audioMixerAddEffect(ulong mixerId, NativeEffectManager.NativeAudioEffect effect);
    [LibraryImport(Lib)] internal static partial void audioMixerRemoveEffect(ulong mixerId, ulong effectId);

    [LibraryImport(Lib)] internal static partial ulong audioMixerCreate(ulong parentId);
    [LibraryImport(Lib)] internal static partial void  audioMixerDispose(ulong mixerId);
    [LibraryImport(Lib)] internal static partial float audioMixerGetVolume(ulong mixerId);
    [LibraryImport(Lib)] internal static partial void  audioMixerSetVolume(ulong mixerId, float volume);
    [LibraryImport(Lib)] internal static partial ulong audioMixerPlay(ulong mixerId, ulong sampleId);
    [LibraryImport(Lib)] internal static partial ulong audioMixerCreatePushStream(ulong mixerId, int sampleRate, int channels);
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
