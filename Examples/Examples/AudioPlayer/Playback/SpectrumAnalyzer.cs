using System.Runtime.InteropServices;
using Rin.Core.Audio;
using Rin.Core.Audio.Effects;

namespace Examples.AudioPlayer.Playback;

/// <summary>
///     Attaches <see cref="SpectrumTapEffect" /> to a group and smooths the band levels it writes for display,
///     along with a slowly falling peak per band, the energy of the lowest bands and a pulse that rises on beats.
/// </summary>
public sealed unsafe class SpectrumAnalyzer : IDisposable
{
    public const int BandCount = 40;
    private const int BassBands = 4;
    private const float AttackPerSecond = 40f;
    private const float ReleasePerSecond = 7f;
    private const float PeakFallPerSecond = 0.45f;
    private const float BassSmoothingPerSecond = 12f;
    private const float BassAveragePerSecond = 1.5f;
    private const float BeatGain = 4f;
    private const float PulseBassShare = 0.4f;

    private readonly float* _bands = (float*)NativeMemory.AllocZeroed(BandCount * sizeof(float));
    private readonly IEffectController<SpectrumTapEffect.Parameters> _effect;
    private readonly float[] _levels = new float[BandCount];
    private readonly float[] _peaks = new float[BandCount];
    private float _bassAverage;
    private bool _disposed;

    public SpectrumAnalyzer(IAudioGroup group)
    {
        _effect = group.AddEffect(SpectrumTapEffect.Descriptor);
        _effect.Parameters = new SpectrumTapEffect.Parameters { Bands = (IntPtr)_bands, BandCount = BandCount };
    }

    public ReadOnlySpan<float> Levels => _levels;

    public ReadOnlySpan<float> Peaks => _peaks;

    public float Bass { get; private set; }

    /// <summary>
    ///     0 to 1: part of the bass energy plus how far the bass currently sits above its recent average, so it
    ///     jumps on a kick and settles between beats.
    /// </summary>
    public float Pulse { get; private set; }

    public void Update(float deltaTime)
    {
        if (_disposed) return;

        var attack = 1f - MathF.Exp(-deltaTime * AttackPerSecond);
        var release = 1f - MathF.Exp(-deltaTime * ReleasePerSecond);
        var bass = 0f;
        for (var i = 0; i < BandCount; i++)
        {
            var target = _bands[i];
            var rate = target > _levels[i] ? attack : release;
            _levels[i] += (target - _levels[i]) * rate;
            _peaks[i] = float.Max(_levels[i], _peaks[i] - deltaTime * PeakFallPerSecond);
            if (i < BassBands) bass += _levels[i];
        }

        Bass += (bass / BassBands - Bass) * (1f - MathF.Exp(-deltaTime * BassSmoothingPerSecond));
        _bassAverage += (Bass - _bassAverage) * (1f - MathF.Exp(-deltaTime * BassAveragePerSecond));
        Pulse = float.Clamp(Bass * PulseBassShare + float.Max(Bass - _bassAverage, 0f) * BeatGain, 0f, 1f);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _effect.Parameters = default;
        _effect.Dispose();
        NativeMemory.Free(_bands);
    }
}
