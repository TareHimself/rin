using System.Numerics;
using Examples.AudioEffects.Effects;
using Examples.Common.Views;
using Rin.Core;
using Rin.Core.Audio;
using Rin.Core.Audio.Effects;
using Rin.Core.Extensions;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.AudioEffects.Views;

/// <summary>
///     Plays an audio file through the master group and switches effects on it.
/// </summary>
public sealed class AudioEffectsView : RectView
{
    private const float CardWidth = 660f;
    private const float RowGap = 10f;
    private const float PresetWidth = 92f;
    private const float FeedbackStep = 0.1f;
    private const float MaxFeedback = 0.97f;
    private const float RootStepSemitones = 2f;

    private static readonly (string Name, EqParameters Gains)[] Presets =
    [
        ("Flat", EqParameters.Flat),
        ("Bass", EqParameters.BassBooster),
        ("Vocal", EqParameters.VocalBooster),
        ("Treble", EqParameters.TrebleBooster),
        ("Rock", EqParameters.Rock),
        ("Dance", EqParameters.Dance)
    ];

    private readonly EffectSlot<EqParameters> _eq;
    private readonly EffectSlot<BassSpiceEffect.Parameters> _bassSpice;
    private readonly EffectSlot<LowPassEffect.Parameters> _lowPass;
    private readonly EffectSlot<StressTestEffect.Parameters> _stress;
    private readonly EffectSlot<BloomEffect.Parameters> _bloom;
    private readonly EffectSlot<NoAudioEffectParameters> _orbit;
    private readonly EffectSlot<NoAudioEffectParameters> _warmth;

    private readonly ActionButton[] _presetButtons = new ActionButton[Presets.Length];
    private readonly ActionButton _playButton;
    private readonly TextBoxView _track = Ui.Label("No file open", 15f, Ui.Muted);
    private readonly TextBoxView _time = Ui.Label("00:00 / 00:00", 14f, Ui.Muted);

    private IAudioSample? _sample;
    private IActiveAudio? _audio;
    private bool _shownPlaying;
    private int _shownSecond;

    public AudioEffectsView(IAudioGroup group)
    {
        _eq = new EffectSlot<EqParameters>(ParametricEq.Descriptor, group);
        _bassSpice = new EffectSlot<BassSpiceEffect.Parameters>(BassSpiceEffect.Descriptor, group);
        _lowPass = new EffectSlot<LowPassEffect.Parameters>(LowPassEffect.Descriptor, group);
        _stress = new EffectSlot<StressTestEffect.Parameters>(StressTestEffect.Descriptor, group);
        _bloom = new EffectSlot<BloomEffect.Parameters>(BloomEffect.Descriptor, group);
        _orbit = new EffectSlot<NoAudioEffectParameters>(OrbitEffect.Descriptor, group);
        _warmth = new EffectSlot<NoAudioEffectParameters>(WarmthEffect.Descriptor, group);
        _playButton = Ui.QuietButton("Play", TogglePlay);

        Color = Ui.Background;
        InitChild = new PanelView
        {
            InitSlots =
            [
                new PanelSlot
                {
                    Child = BuildCard(),
                    MinAnchor = new Vector2(0.5f),
                    MaxAnchor = new Vector2(0.5f),
                    Alignment = new Vector2(0.5f),
                    SizeToContent = true
                }
            ]
        };
    }

    private CardView BuildCard()
    {
        return new CardView
        {
            Color = Ui.Surface,
            BorderColor = Ui.Border,
            ShadowAlpha = 0.5f,
            BorderRadius = BorderRadius.All(22f),
            Padding = new Padding(32f),
            InitChild = new SizerView
            {
                WidthOverride = CardWidth,
                InitChild = Ui.Column(
                    Ui.Slot(Ui.Label("Audio effects", 32f)),
                    Ui.Slot(Ui.Gap(6f)),
                    Ui.Slot(Ui.Label("Effects run on the master group in the order they were switched on.", 14f,
                        Ui.Muted)),
                    Ui.Slot(Ui.Gap(20f)),
                    Ui.Slot(BuildTransport()),
                    Ui.Slot(Ui.Gap(12f)),
                    Ui.Slot(BuildSeekBar()),
                    Ui.Slot(Ui.Gap(22f)),
                    Ui.Slot(new EffectRowView(_eq, "Parametric EQ", "Six bands, gains from a preset")),
                    Ui.Slot(Ui.Gap(8f)),
                    Ui.Slot(BuildPresets()),
                    Ui.Slot(Ui.Gap(RowGap)),
                    Ui.Slot(new EffectRowView(_bassSpice, "Bass spice", "Saturated low end and a wider stereo image")),
                    Ui.Slot(Ui.Gap(RowGap)),
                    Ui.Slot(new EffectRowView(_lowPass, "Low pass", "One pole filter at 600 Hz")),
                    Ui.Slot(Ui.Gap(RowGap)),
                    Ui.Slot(new EffectRowView(_stress, "Stress test", "Modulated delay with filtered feedback",
                        new StepperView(StepFeedback))),
                    Ui.Slot(Ui.Gap(RowGap)),
                    Ui.Slot(new EffectRowView(_bloom, "Bloom", "Resonators tuned to a chord above the root",
                        new StepperView(StepRoot))),
                    Ui.Slot(Ui.Gap(RowGap)),
                    Ui.Slot(new EffectRowView(_orbit, "Orbit", "Mono mix panned around once a second")),
                    Ui.Slot(Ui.Gap(RowGap)),
                    Ui.Slot(new EffectRowView(_warmth, "Warmth", "Soft saturation")))
            }
        };
    }

    private FlexBoxView BuildTransport()
    {
        return Ui.Row(
            Controls.Fixed(Ui.PrimaryButton("Open audio", PickFile), 130f),
            Ui.Slot(Ui.Gap(0f, 10f)),
            Controls.Fixed(_playButton, 100f),
            Ui.Slot(Ui.Gap(0f, 14f)),
            Ui.Slot(_track, 1f, CrossFit.Desired, CrossAlign.Center),
            Ui.Slot(_time, fit: CrossFit.Desired, align: CrossAlign.Center));
    }

    private SizerView BuildSeekBar()
    {
        return new SizerView
        {
            HeightOverride = 8f,
            InitChild = new ProgressBarView(GetProgress, Seek)
            {
                BackgroundColor = Ui.Raised,
                ForegroundColor = Ui.Accent,
                BorderRadius = BorderRadius.All(4f)
            }
        };
    }

    private FlexBoxView BuildPresets()
    {
        var row = new FlexBoxView { Axis = Axis.Row };
        for (var i = 0; i < Presets.Length; i++)
        {
            var index = i;
            _presetButtons[i] = Controls.Compact(Presets[i].Name, () => SelectPreset(index));
            if (i > 0) row.Add(Ui.Slot(Ui.Gap(0f, 8f)));
            row.Add(Controls.Fixed(_presetButtons[i], PresetWidth));
        }

        SelectPreset(0);
        return row;
    }

    public override void Dispose()
    {
        base.Dispose();
        CloseAudio();
        _eq.Dispose();
        _bassSpice.Dispose();
        _lowPass.Dispose();
        _stress.Dispose();
        _bloom.Dispose();
        _orbit.Dispose();
        _warmth.Dispose();
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (_audio is null) return;
        if ((int)_audio.Position == _shownSecond && _audio.IsPlaying == _shownPlaying) return;

        ShowTransport();
    }

    private void SelectPreset(int index)
    {
        _eq.Parameters = Presets[index].Gains;
        for (var i = 0; i < _presetButtons.Length; i++) Controls.Highlight(_presetButtons[i], i == index);
    }

    private string StepFeedback(int direction)
    {
        var parameters = _stress.Parameters;
        parameters.Feedback = float.Clamp(parameters.Feedback + direction * FeedbackStep, 0f, MaxFeedback);
        _stress.Parameters = parameters;
        return $"Feedback {parameters.Feedback:0.00}";
    }

    private string StepRoot(int direction)
    {
        var parameters = _bloom.Parameters;
        var ratio = MathF.Pow(2f, direction * RootStepSemitones / 12f);
        parameters.RootHz = float.Clamp(parameters.RootHz * ratio, 40f, 1000f);
        _bloom.Parameters = parameters;
        return $"Root {parameters.RootHz:0} Hz";
    }

    private void PickFile()
    {
        Task.Run(() => IApplication.Get().SelectFile("Select an audio file", filter: "*.wav;*.ogg;*.flac;*.mp3"))
            .DispatchMain(files =>
            {
                if (files.Length > 0) Open(files[0]);
            });
    }

    private void Open(string file)
    {
        CloseAudio();
        _sample = IAudioModule.Get().MakeStream(file);
        _audio = _sample.MakeActive();
        _audio.Play();
        _track.Content = Path.GetFileNameWithoutExtension(file);
        ShowTransport();
    }

    private void CloseAudio()
    {
        _audio?.Dispose();
        _audio = null;
        _sample?.Dispose();
        _sample = null;
    }

    private void TogglePlay()
    {
        if (_audio is null) return;

        if (_audio.IsPlaying)
            _audio.Pause();
        else
            _audio.Play(_audio.HasEnded);
        ShowTransport();
    }

    private float GetProgress()
    {
        return _audio is { Duration: > 0.0 } audio ? (float)(audio.Position / audio.Duration) : 0f;
    }

    private void Seek(float progress)
    {
        _audio?.SetPosition(progress * _audio.Duration);
    }

    private void ShowTransport()
    {
        if (_audio is null) return;

        _shownPlaying = _audio.IsPlaying;
        _shownSecond = (int)_audio.Position;
        _playButton.SetText(_shownPlaying ? "Pause" : "Play");
        _time.Content = $"{FormatTime(_audio.Position)} / {FormatTime(_audio.Duration)}";
    }

    private static string FormatTime(double seconds)
    {
        var whole = (int)double.Max(0.0, seconds);
        return $"{whole / 60:00}:{whole % 60:00}";
    }
}
