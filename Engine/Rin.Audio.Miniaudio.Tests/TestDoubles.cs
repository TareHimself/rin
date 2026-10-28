using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rin.Core;
using Rin.Core.Audio.Effects;

namespace Rin.Audio.Miniaudio.Tests;

/// <summary>
///     Stands in for the native library: every call lands in <see cref="Calls" /> in order, handles are counters,
///     and <see cref="FailNext" /> makes the next fallible call throw the way a checked wrapper would.
/// </summary>
internal sealed class RecordingEngine : IMiniaudioEngine
{
    private readonly Dictionary<ulong, NativeEffectManager.NativeAudioEffect> _effects = new();
    private MiniaudioEngine.AudioResult? _failNext;
    private ulong _nextHandle = 100;

    public List<string> Calls { get; } = [];
    public List<byte[]> LoadedBytes { get; } = [];
    public List<byte[]> Pushed { get; } = [];
    public Dictionary<ulong, float> MixerVolumes { get; } = new();
    public bool ActiveResult { get; set; } = true;
    public double Position { get; set; }
    public double Duration { get; set; }

    public float Volume
    {
        get => field;
        set
        {
            Record($"SetVolume({value})");
            field = value;
        }
    }

    public ulong MasterMixer => 1;

    public void FailNext(MiniaudioEngine.AudioResult result) => _failNext = result;

    public void Init() => Fallible("Init");

    public void Shutdown() => Record("Shutdown");

    public ulong SampleLoad(string filePath) => Create($"SampleLoad({filePath})");

    public ulong SampleLoad(ReadOnlySpan<byte> data)
    {
        LoadedBytes.Add(data.ToArray());
        return Create($"SampleLoad({data.Length} bytes)");
    }

    public ulong StreamOpen(string filePath) => Create($"StreamOpen({filePath})");

    public ulong StreamOpen(ReadOnlySpan<byte> data)
    {
        LoadedBytes.Add(data.ToArray());
        return Create($"StreamOpen({data.Length} bytes)");
    }

    public ulong SampleMakeActive(ulong sample) => Create($"SampleMakeActive({sample})");

    public void SampleDispose(ulong sample) => Record($"SampleDispose({sample})");

    public ulong MixerCreate(ulong parent) => Create($"MixerCreate({parent})");

    public void MixerDispose(ulong mixer) => Record($"MixerDispose({mixer})");

    public float MixerGetVolume(ulong mixer)
    {
        Record($"MixerGetVolume({mixer})");
        return MixerVolumes.GetValueOrDefault(mixer, 1f);
    }

    public void MixerSetVolume(ulong mixer, float volume)
    {
        Record($"MixerSetVolume({mixer}, {volume})");
        MixerVolumes[mixer] = volume;
    }

    public ulong MixerPlay(ulong mixer, ulong sample) => Create($"MixerPlay({mixer}, {sample})");

    public ulong MixerCreatePushStream(ulong mixer, int sampleRate, int channels) =>
        Create($"MixerCreatePushStream({mixer}, {sampleRate}, {channels})");

    public void MixerAddEffect(ulong mixer, in NativeEffectManager.NativeAudioEffect effect)
    {
        Fallible($"MixerAddEffect({mixer}, {effect.EffectId})");
        _effects[effect.EffectId] = effect;
    }

    // The real library releases the descriptor's buffers through the effect's callback when it drops the effect.
    public unsafe void MixerRemoveEffect(ulong mixer, ulong effect)
    {
        Record($"MixerRemoveEffect({mixer}, {effect})");
        if (!_effects.Remove(effect, out var native)) return;
        native.ReleaseCallback(native.DescriptorHandle, native.Parameters, native.State);
    }

    public bool ActiveIsPlaying(ulong active) => Query($"ActiveIsPlaying({active})");

    public bool ActiveHasEnded(ulong active) => Query($"ActiveHasEnded({active})");

    public double ActiveGetPosition(ulong active)
    {
        Record($"ActiveGetPosition({active})");
        return Position;
    }

    public double ActiveGetDuration(ulong active)
    {
        Record($"ActiveGetDuration({active})");
        return Duration;
    }

    public bool ActivePlay(ulong active, bool restart) => Query($"ActivePlay({active}, {restart})");

    public bool ActivePause(ulong active) => Query($"ActivePause({active})");

    public bool ActiveSetVolume(ulong active, float volume) => Query($"ActiveSetVolume({active}, {volume})");

    public bool ActiveSetPosition(ulong active, double seconds) => Query($"ActiveSetPosition({active}, {seconds})");

    public void ActiveDispose(ulong active) => Record($"ActiveDispose({active})");

    public nuint PushStreamPush(ulong stream, ReadOnlySpan<byte> data)
    {
        Record($"PushStreamPush({stream}, {data.Length} bytes)");
        Pushed.Add(data.ToArray());
        return (nuint)data.Length;
    }

    private void Record(string call) => Calls.Add(call);

    private bool Query(string call)
    {
        Record(call);
        return ActiveResult;
    }

    private void Fallible(string call)
    {
        Record(call);
        if (_failNext is not { } result) return;
        _failNext = null;
        var paren = call.IndexOf('(');
        throw new NativeException("Rin.Audio.Miniaudio.Native", paren < 0 ? call : call[..paren], result);
    }

    private ulong Create(string call)
    {
        Fallible(call);
        return _nextHandle++;
    }
}

internal struct FakeEffectParameters
{
    public float Gain;
}

/// <summary>
///     Hand-written stand-in for a generated descriptor. The parameter buffer is a real
///     <see cref="ParameterSlots{T}" /> so the controller can write into it.
/// </summary>
internal sealed class FakeEffectDescriptor : IAudioEffectDescriptor<FakeEffectParameters>
{
    public int ReleasedParameters { get; private set; }
    public int ReleasedStates { get; private set; }

    public IntPtr GetProcessMethodPtr() => IntPtr.Zero;

    public IntPtr CreateState() => IntPtr.Zero;

    public unsafe IntPtr CreateParameters()
    {
        var ptr = (IntPtr)NativeMemory.Alloc((nuint)sizeof(ParameterSlots<FakeEffectParameters>));
        Unsafe.Write(ptr.ToPointer(), new ParameterSlots<FakeEffectParameters>(default));
        return ptr;
    }

    public void ReleaseState(IntPtr state) => ReleasedStates++;

    public unsafe void ReleaseParameters(IntPtr parameters)
    {
        ReleasedParameters++;
        NativeMemory.Free(parameters.ToPointer());
    }
}
