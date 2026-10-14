using System.Numerics;
using Examples.AudioPlayer.Playback;
using Examples.Common.Views;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.AudioPlayer.Views;

/// <summary>
///     The current track: cover, names, spectrum, seek bar and transport controls.
/// </summary>
public sealed class NowPlayingView : FlexBoxView
{
    public const float Width = 360f;
    private const float CoverRadius = 18f;
    private const float SmallButton = 40f;
    private const float PlayButton = 62f;
    private const float VolumeWidth = 90f;
    private const float MuteThreshold = 0.01f;
    private const float LowVolumeThreshold = 0.5f;

    private static readonly Color Quiet = Color.White with { A = 0.08f };
    private static readonly Color QuietHover = Color.White with { A = 0.16f };
    private static readonly Color QuietPressed = Color.White with { A = 0.24f };

    private readonly Player _player;
    private readonly SpectrumAnalyzer _analyzer;
    private readonly CoverArtView _cover = new(Width, CoverRadius);
    private readonly TextBoxView _title = Ui.Label("Nothing playing", 26f);
    private readonly TextBoxView _subtitle = Ui.Label("Add some tracks to the queue", 15f, Ui.Muted);
    private readonly TextBoxView _position = Ui.Label("0:00", 13f, Ui.Muted);
    private readonly TextBoxView _duration = Ui.Label("0:00", 13f, Ui.Muted);
    private readonly SliderView _seek;
    private readonly IconButton _play;
    private readonly IconButton _repeat;
    private readonly IconButton _volumeIcon;
    private float _volumeBeforeMute;
    private bool _shownPlaying;
    private int _shownSecond = -1;

    public NowPlayingView(Player player, SpectrumAnalyzer analyzer)
    {
        _player = player;
        _analyzer = analyzer;
        _seek = new SliderView(() => (float)(player.Duration > 0.0 ? player.Position / player.Duration : 0.0),
            fraction => player.Seek(fraction * player.Duration)) { TrackColor = Quiet };
        _play = new IconButton(Icons.Play, PlayButton, 24f, Color.White, Color.FromRgb(0.9f, 0.92f, 1f),
            Color.FromRgb(0.8f, 0.83f, 0.95f), Ui.Background);
        _repeat = QuietButton(Icons.Repeat, 15f);
        _volumeIcon = QuietButton(Icons.VolumeHigh, 15f);
        _title.WrapContent = true;
        _subtitle.WrapContent = true;

        _play.Clicked += player.TogglePlay;
        _repeat.Clicked += ToggleRepeat;
        _volumeIcon.Clicked += ToggleMute;

        Axis = Axis.Column;
        InitSlots =
        [
            Ui.Slot(new SizerView { WidthOverride = Width, HeightOverride = Width, InitChild = _cover }),
            Ui.Slot(Ui.Gap(22f)),
            Ui.Slot(_title),
            Ui.Slot(Ui.Gap(4f)),
            Ui.Slot(_subtitle),
            Ui.Slot(Ui.Gap(18f)),
            Ui.Slot(new SpectrumView(analyzer) { Height = 56f }),
            Ui.Slot(Ui.Gap(14f)),
            Ui.Slot(_seek),
            Ui.Slot(Ui.Gap(6f)),
            Ui.Slot(Ui.Row(
                Ui.Slot(_position, 1f, CrossFit.Desired),
                Ui.Slot(_duration, fit: CrossFit.Desired))),
            Ui.Slot(Ui.Gap(16f)),
            Ui.Slot(BuildTransport())
        ];

        player.TrackChanged += ShowTrack;
        ShowTrack();
        ShowVolume();
    }

    private FlexBoxView BuildTransport()
    {
        var previous = QuietButton(Icons.Previous, 16f);
        var next = QuietButton(Icons.Next, 16f);
        previous.Clicked += _player.Previous;
        next.Clicked += _player.Next;

        var volume = new SliderView(() => _player.Volume, SetVolume)
        {
            TrackHeight = 4f,
            KnobRadius = 6f,
            TrackColor = Quiet,
            FillColor = Color.White with { A = 0.7f }
        };

        return Ui.Row(
            Ui.Slot(_repeat, fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 0f), 1f),
            Ui.Slot(previous, fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 10f)),
            Ui.Slot(_play, fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 10f)),
            Ui.Slot(next, fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 0f), 1f),
            Ui.Slot(_volumeIcon, fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 6f)),
            Ui.Slot(new SizerView { WidthOverride = VolumeWidth, InitChild = volume }, fit: CrossFit.Desired,
                align: CrossAlign.Center));
    }

    private static IconButton QuietButton(char icon, float iconSize)
    {
        return new IconButton(icon, SmallButton, iconSize, Quiet, QuietHover, QuietPressed, Ui.Text);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        _player.Update();
        _analyzer.Update(deltaTime);

        var second = (int)_player.Position;
        if (second == _shownSecond && _player.IsPlaying == _shownPlaying) return;

        _shownSecond = second;
        _shownPlaying = _player.IsPlaying;
        _position.Content = QueueRowView.FormatTime(_player.Position);
        _play.Icon = _shownPlaying ? Icons.Pause : Icons.Play;
    }

    private void ShowTrack()
    {
        var entry = _player.Current;
        _cover.Show(entry);
        _shownSecond = -1;

        if (entry is null)
        {
            _title.Content = "Nothing playing";
            _subtitle.Content = "Add some tracks to the queue";
            _duration.Content = "0:00";
            return;
        }

        var info = entry.Info;
        _title.Content = info.Title;
        _subtitle.Content = (info.Artist, info.Album) switch
        {
            ({ Length: > 0 } artist, { Length: > 0 } album) => $"{artist}  ·  {album}",
            ({ Length: > 0 } artist, _) => artist,
            (_, { Length: > 0 } album) => album,
            _ => "Unknown artist"
        };
        _duration.Content = QueueRowView.FormatTime(_player.Duration);
    }

    private void ToggleRepeat()
    {
        _player.Repeat = !_player.Repeat;
        if (_player.Repeat)
            _repeat.Restyle(Ui.Accent with { A = 0.3f }, Ui.Accent with { A = 0.4f }, Ui.Accent with { A = 0.5f },
                Ui.Accent);
        else
            _repeat.Restyle(Quiet, QuietHover, QuietPressed, Ui.Text);
    }

    private void ToggleMute()
    {
        if (_player.Volume > MuteThreshold)
        {
            _volumeBeforeMute = _player.Volume;
            SetVolume(0f);
        }
        else
        {
            SetVolume(_volumeBeforeMute > MuteThreshold ? _volumeBeforeMute : LowVolumeThreshold);
        }
    }

    private void SetVolume(float volume)
    {
        _player.Volume = volume;
        ShowVolume();
    }

    private void ShowVolume()
    {
        var volume = _player.Volume;
        _volumeIcon.Icon = volume <= MuteThreshold ? Icons.VolumeMuted
            : volume < LowVolumeThreshold ? Icons.VolumeLow
            : Icons.VolumeHigh;
    }

    public override void Dispose()
    {
        _player.TrackChanged -= ShowTrack;
        base.Dispose();
    }
}
