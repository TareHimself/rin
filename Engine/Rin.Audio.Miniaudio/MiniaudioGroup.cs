using Rin.Core.Audio;
using Rin.Core.Audio.Effects;
using Rin.Core.Shared;

namespace Rin.Audio.Miniaudio;

public class MiniaudioAudioGroup : IAudioGroup
{
    private readonly IMiniaudioEngine _engine;
    private readonly bool _isMaster;
    private readonly IdFactory<ulong> _idFactory = new();
    private readonly Dictionary<ulong, IEffectController> _effectControllers = [];
    private bool _disposed;

    internal MiniaudioAudioGroup(IMiniaudioEngine engine, ulong id, bool isMaster = false)
    {
        _engine = engine;
        Id = id;
        _isMaster = isMaster;
    }

    internal ulong Id { get; }

    public float Volume
    {
        get
        {
            ThrowIfDisposed();
            return _engine.MixerGetVolume(Id);
        }
        set
        {
            ThrowIfDisposed();
            _engine.MixerSetVolume(Id, value);
        }
    }

    public IChannel Play(IAudioSample sample)
    {
        ThrowIfDisposed();
        return new MiniaudioActiveAudio(_engine, _engine.MixerPlay(Id, SampleId(sample)));
    }

    public IPushStream CreatePushStream(int frequency, int channels)
    {
        ThrowIfDisposed();
        return new MiniaudioPushStream(_engine, _engine.MixerCreatePushStream(Id, frequency, channels));
    }

    public IAudioGroup CreateSubGroup()
    {
        ThrowIfDisposed();
        return new MiniaudioAudioGroup(_engine, _engine.MixerCreate(Id));
    }

    public IEffectController<TParams> AddEffect<TParams>(IAudioEffectDescriptor<TParams> descriptor)
        where TParams : unmanaged
    {
        ThrowIfDisposed();
        var effectId = _idFactory.NewId();
        var effect = new NativeEffectManager.NativeAudioEffect(effectId, descriptor);
        _engine.MixerAddEffect(Id, effect);

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
        _engine.MixerRemoveEffect(Id, effectId);
        controller.Dispose();
    }

    public void Dispose()
    {
        if (_disposed || _isMaster) return;
        _disposed = true;
        GC.SuppressFinalize(this);
        foreach (var effectId in _effectControllers.Keys.ToArray()) RemoveEffect(effectId);
        _engine.MixerDispose(Id);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private static ulong SampleId(IAudioSample sample)
    {
        if (sample is not MiniaudioAudioSample miniaudioSample)
            throw new ArgumentException("must be MiniaudioAudioSample", nameof(sample));
        return miniaudioSample.NativeId;
    }
}
