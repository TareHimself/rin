using Rin.Core.Audio;
using Rin.Core.Audio.Effects;
using Rin.Core.Shared;

namespace Rin.Audio.Miniaudio;

public class MiniaudioAudioGroup : IAudioGroup
{
    private readonly bool _isMaster;
    private bool _disposed;
    private readonly IdFactory<ulong> _idFactory = new ();
    private readonly Dictionary<ulong,IEffectController>  _effectControllers = [];

    internal MiniaudioAudioGroup(ulong id, bool isMaster = false)
    {
        Id = id;
        _isMaster = isMaster;
    }

    private ulong Id { get; }

    public float Volume
    {
        get { ThrowIfDisposed(); return Native.audioMixerGetVolume(Id); }
        set { ThrowIfDisposed(); Native.audioMixerSetVolume(Id, value); }
    }

    public IChannel Play(IAudioSample sample)
    {
        ThrowIfDisposed();
        return new MiniaudioActiveAudio(Native.MixerPlay(Id, SampleId(sample)));
    }

    public IPushStream CreatePushStream(int frequency, int channels)
    {
        ThrowIfDisposed();
        return new MiniaudioPushStream(Native.MixerCreatePushStream(Id, frequency, channels));
    }

    public IAudioGroup CreateSubGroup()
    {
        ThrowIfDisposed();
        return new MiniaudioAudioGroup(Native.MixerCreate(Id));
    }

    // public IDirectionalAudioGroup CreateChildDirectionalBus()
    // {
    //     ThrowIfDisposed();
    //     return new MiniaudioDirectionalAudioMixer(Native.audioCreateScene(_id));
    // }

    public void Dispose()
    {
        if (_disposed || _isMaster) return;
        _disposed = true;
        GC.SuppressFinalize(this);
        foreach (var effectId in _effectControllers.Keys.ToArray()) RemoveEffect(effectId);
        Native.audioMixerDispose(Id);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private static ulong SampleId(IAudioSample s)
    {
        if (s is not MiniaudioAudioSample m)
            throw new ArgumentException("must be MiniaudioAudioSample", nameof(s));
        return m.NativeId;
    }

    public IEffectController<TParams> AddEffect<TParams>(IAudioEffectDescriptor<TParams> descriptor) where TParams : unmanaged
    {
        ThrowIfDisposed();
        var effectId = _idFactory.NewId();
        var effect = new NativeEffectManager.NativeAudioEffect(effectId, descriptor);
        Native.MixerAddEffect(Id, effect);

        var controller = new MiniaudioEffectController<TParams>(this, effectId, effect.Parameters);
        _effectControllers.Add(effectId, controller);
        return controller;
    }

    /// <summary>
    ///     Removing through the group and disposing the controller both end here, so the dictionary entry is dropped
    ///     first to stop the two from calling each other again.
    /// </summary>
    public void RemoveEffect(ulong effectId)
    {
        if (!_effectControllers.Remove(effectId, out var controller)) return;
        Native.audioMixerRemoveEffect(Id, effectId);
        controller.Dispose();
    }
}
