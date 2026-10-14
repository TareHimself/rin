using System.Runtime.InteropServices;
using Rin.Core.Audio;
using Rin.Core.Audio.Effects;

namespace Examples.AudioPlayer.Playback;

/// <summary>
///     Attaches <see cref="SpectrumTapEffect" /> to a group and smooths the band levels it writes for display.
/// </summary>
public sealed unsafe class SpectrumAnalyzer : IDisposable
{
    public const int BandCount = 40;
    private const float AttackPerSecond = 40f;
    private const float ReleasePerSecond = 7f;

    private readonly float* _bands = (float*)NativeMemory.AllocZeroed(BandCount * sizeof(float));
    private readonly IEffectController<SpectrumTapEffect.Parameters> _effect;
    private readonly float[] _levels = new float[BandCount];
    private bool _disposed;

    public SpectrumAnalyzer(IAudioGroup group)
    {
        _effect = group.AddEffect(SpectrumTapEffect.Descriptor);
        _effect.Parameters = new SpectrumTapEffect.Parameters { Bands = (IntPtr)_bands, BandCount = BandCount };
    }

    public ReadOnlySpan<float> Levels => _levels;

    public void Update(float deltaTime)
    {
        if (_disposed) return;

        var attack = 1f - MathF.Exp(-deltaTime * AttackPerSecond);
        var release = 1f - MathF.Exp(-deltaTime * ReleasePerSecond);
        for (var i = 0; i < BandCount; i++)
        {
            var target = _bands[i];
            var rate = target > _levels[i] ? attack : release;
            _levels[i] += (target - _levels[i]) * rate;
        }
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
