using Examples.AudioEffects.Effects;
using Rin.Core.Audio;
using Rin.Core.Audio.Effects;

namespace Examples.AudioPlayer.Playback;

/// <summary>
///     A six band <see cref="ParametricEq" /> on a group that stays attached, so it keeps its place in the effect
///     chain. Switching it off applies flat gains and keeps the user's gains for when it comes back on.
/// </summary>
public sealed class Equalizer : IDisposable
{
    public const int BandCount = EqState.Bands;
    public const float MaxGainDb = 12f;

    private readonly IEffectController<EqParameters> _effect;
    private EqParameters _gains = EqParameters.Flat;
    private bool _enabled = true;

    public Equalizer(IAudioGroup group)
    {
        _effect = group.AddEffect(ParametricEq.Descriptor);
        Apply();
    }

    public static IReadOnlyList<float> BandHz => EqCoefficients.BandHz;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            Apply();
        }
    }

    public EqParameters Gains
    {
        get => _gains;
        set
        {
            _gains = value;
            Apply();
        }
    }

    public float GetGain(int band)
    {
        return _gains[band];
    }

    public void SetGain(int band, float db)
    {
        _gains[band] = float.Clamp(db, -MaxGainDb, MaxGainDb);
        Apply();
    }

    private void Apply()
    {
        _effect.Parameters = _enabled ? _gains : EqParameters.Flat;
    }

    public void Dispose()
    {
        _effect.Dispose();
    }
}
