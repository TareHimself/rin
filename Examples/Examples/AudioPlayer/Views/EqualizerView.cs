using System.Numerics;
using Examples.AudioEffects.Effects;
using Examples.AudioPlayer.Playback;
using Examples.Common.Views;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.AudioPlayer.Views;

/// <summary>
///     The equalizer card: presets and an on/off switch above one vertical gain slider per band.
/// </summary>
public sealed class EqualizerView : CardView
{
    private const float SliderLength = 104f;
    private const float PresetWidth = 72f;
    private const float ToggleWidth = 56f;

    private static readonly (string Name, EqParameters Gains)[] Presets =
    [
        ("Flat", EqParameters.Flat),
        ("Bass", EqParameters.BassBooster),
        ("Vocal", EqParameters.VocalBooster),
        ("Treble", EqParameters.TrebleBooster),
        ("Rock", EqParameters.Rock),
        ("Dance", EqParameters.Dance)
    ];

    private readonly Equalizer _equalizer;
    private readonly ActionButton[] _presetButtons = new ActionButton[Presets.Length];
    private readonly ActionButton _toggle;

    public EqualizerView(Equalizer equalizer)
    {
        _equalizer = equalizer;
        _toggle = Controls.Compact("On", ToggleEnabled);
        Color = PlayerView.Glass;
        BorderColor = PlayerView.GlassBorder;
        BorderRadius = BorderRadius.All(22f);
        Padding = new Padding(20f, 16f);

        SetChild(Ui.Column(
            Ui.Slot(BuildHeader()),
            Ui.Slot(Ui.Gap(14f)),
            Ui.Slot(BuildBands())));

        ShowEnabled();
        HighlightPreset();
    }

    private FlexBoxView BuildHeader()
    {
        var header = Ui.Row(
            Ui.Slot(Ui.Label("Equalizer", 20f), fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 0f), 1f));

        for (var i = 0; i < Presets.Length; i++)
        {
            var index = i;
            _presetButtons[i] = Controls.Compact(Presets[i].Name, () => SelectPreset(index));
            header.Add(Controls.Fixed(_presetButtons[i], PresetWidth));
            header.Add(Ui.Slot(Ui.Gap(0f, 6f)));
        }

        header.Add(Ui.Slot(Ui.Gap(0f, 8f)));
        header.Add(Controls.Fixed(_toggle, ToggleWidth));
        return header;
    }

    private FlexBoxView BuildBands()
    {
        var row = new FlexBoxView { Axis = Axis.Row };
        for (var band = 0; band < Equalizer.BandCount; band++)
        {
            var index = band;
            var slider = new SliderView(() => ToFraction(_equalizer.GetGain(index)), f => SetGain(index, f))
            {
                Axis = Axis.Column,
                TrackHeight = 5f,
                KnobRadius = 7f,
                TrackColor = Color.White with { A = 0.08f },
                FillOrigin = 0.5f,
                KnobAlwaysVisible = true,
                LiveUpdate = true
            };

            var column = Ui.Column(
                Ui.Slot(new SizerView { HeightOverride = SliderLength, InitChild = slider }, fit: CrossFit.Desired,
                    align: CrossAlign.Center),
                Ui.Slot(Ui.Gap(6f)),
                Ui.Slot(Ui.Label(BandLabel(Equalizer.BandHz[band]), 12f, Ui.Muted), fit: CrossFit.Desired,
                    align: CrossAlign.Center));
            row.Add(Ui.Slot(column, 1f));
        }

        return row;
    }

    private void SetGain(int band, float fraction)
    {
        _equalizer.SetGain(band, (fraction * 2f - 1f) * Equalizer.MaxGainDb);
        HighlightPreset();
    }

    private static float ToFraction(float gainDb)
    {
        return (gainDb / Equalizer.MaxGainDb + 1f) / 2f;
    }

    private void SelectPreset(int index)
    {
        _equalizer.Gains = Presets[index].Gains;
        if (!_equalizer.Enabled) _equalizer.Enabled = true;
        ShowEnabled();
        HighlightPreset();
    }

    private void HighlightPreset()
    {
        var gains = _equalizer.Gains;
        for (var i = 0; i < Presets.Length; i++)
            Controls.Highlight(_presetButtons[i], _equalizer.Enabled && Presets[i].Gains.Equals(gains));
    }

    private void ToggleEnabled()
    {
        _equalizer.Enabled = !_equalizer.Enabled;
        ShowEnabled();
        HighlightPreset();
    }

    private void ShowEnabled()
    {
        _toggle.SetText(_equalizer.Enabled ? "On" : "Off");
        Controls.Highlight(_toggle, _equalizer.Enabled);
    }

    private static string BandLabel(float hz)
    {
        return hz >= 1000f ? $"{hz / 1000f:0.#}k" : $"{hz:0}";
    }
}
