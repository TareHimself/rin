namespace Rin.Audio.Miniaudio;

/// <summary>
///     The only place the audio backend talks to the native library. Every method is one export, so the types
///     above it hold handles and bookkeeping only and tests can drive them with a recording implementation.
/// </summary>
internal interface IMiniaudioEngine
{
    float Volume { get; set; }
    ulong MasterMixer { get; }

    void Init();
    void Shutdown();

    ulong SampleLoad(string filePath);
    ulong SampleLoad(ReadOnlySpan<byte> data);
    ulong StreamOpen(string filePath);
    ulong StreamOpen(ReadOnlySpan<byte> data);
    ulong SampleMakeActive(ulong sample);
    void SampleDispose(ulong sample);

    ulong MixerCreate(ulong parent);
    void MixerDispose(ulong mixer);
    float MixerGetVolume(ulong mixer);
    void MixerSetVolume(ulong mixer, float volume);
    ulong MixerPlay(ulong mixer, ulong sample);
    ulong MixerCreatePushStream(ulong mixer, int sampleRate, int channels);
    void MixerAddEffect(ulong mixer, in NativeEffectManager.NativeAudioEffect effect);
    void MixerRemoveEffect(ulong mixer, ulong effect);

    bool ActiveIsPlaying(ulong active);
    bool ActiveHasEnded(ulong active);
    double ActiveGetPosition(ulong active);
    double ActiveGetDuration(ulong active);
    bool ActivePlay(ulong active, bool restart);
    bool ActivePause(ulong active);
    bool ActiveSetVolume(ulong active, float volume);
    bool ActiveSetPosition(ulong active, double seconds);
    void ActiveDispose(ulong active);

    nuint PushStreamPush(ulong stream, ReadOnlySpan<byte> data);
}
