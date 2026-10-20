using Examples.Common;
using System.Numerics;
using Examples.AudioPlayer.Library;
using Examples.AudioPlayer.Playback;
using Examples.Common.Views;
using Rin.Core;
using Rin.Core.Audio;
using Rin.Core.Extensions;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.AudioPlayer.Views;

/// <summary>
///     The whole player: the current cover blurred across the window, the now playing column, and on the right
///     the queue or the albums above the equalizer.
/// </summary>
public sealed class PlayerView : PanelView
{
    public static readonly Color Glass = new(0.05f, 0.06f, 0.08f, 0.62f);
    public static readonly Color GlassBorder = Color.White with { A = 0.08f };

    private const float Margin = 36f;
    private const float ColumnGap = 32f;
    private const float BackdropBlurRadius = 40f;
    private static readonly Color BackdropTint = Color.White with { A = 0.7f };
    private static readonly Color BackdropShade = Ui.Background with { A = 0.35f };

    private const float RowGap = 16f;

    private static readonly Log Log = Log.For<PlayerView>();

    private readonly Player _player = new();
    private readonly MusicLibrary _library = new();
    private readonly Equalizer _equalizer;
    private readonly SpectrumAnalyzer _analyzer;
    private readonly CoverImageView _backdrop = new() { Tint = BackdropTint, Visibility = Visibility.Hidden };
    private readonly NowPlayingView _nowPlaying;
    private TrackCover? _backdropCover;

    public PlayerView()
    {
        // Effects run in the order they were added, so the spectrum shows the equalized output.
        var master = IAudioModule.Get().MasterAudioGroup;
        _equalizer = new Equalizer(master);
        _analyzer = new SpectrumAnalyzer(master);

        var tabs = new PaneTabsView(["Queue", "Albums"]);
        var panes = new SwitcherView();
        panes.Add(new Slot { Child = new QueueView(_player, () => PickFiles()) });
        panes.Add(new Slot { Child = new AlbumsView(_library, _player, () => PickFiles(false)) });
        tabs.Changed += index => panes.SelectedIndex = index;
        _nowPlaying = new NowPlayingView(_player, _analyzer);

        InitSlots =
        [
            Fill(new RectView { Color = Ui.Background }),
            Fill(_backdrop),
            Fill(new BackgroundBlurView
            {
                Strength = 1f,
                Radius = BackdropBlurRadius,
                InitChild = new RectView { Color = BackdropShade }
            }),
            Fill(new RectView
            {
                Color = Ui.Clear,
                Padding = new Padding(Margin),
                InitChild = Ui.Row(
                    Ui.Slot(
                        new SizerView
                        {
                            WidthOverride = NowPlayingView.Width,
                            InitChild = _nowPlaying
                        }, fit: CrossFit.Desired, align: CrossAlign.Center),
                    Ui.Slot(Ui.Gap(0f, ColumnGap)),
                    Ui.Slot(Ui.Column(
                        Ui.Slot(tabs, fit: CrossFit.Desired),
                        Ui.Slot(Ui.Gap(10f)),
                        Ui.Slot(panes, 1f),
                        Ui.Slot(Ui.Gap(RowGap)),
                        Ui.Slot(new EqualizerView(_equalizer))), 1f))
            })
        ];

        _player.TrackChanged += ShowBackdrop;
    }

    public void TogglePlay()
    {
        _player.TogglePlay();
    }

    public void Next()
    {
        _player.Next();
    }

    public void Previous()
    {
        _player.Previous();
    }

    public void NextVisualizer()
    {
        _nowPlaying.NextVisualizer();
    }

    /// <summary>
    ///     Reads the tags of every audio file among the paths on a worker and adds them to the library, and with
    ///     <paramref name="queue" /> also queues them in order.
    /// </summary>
    public void AddFiles(IEnumerable<string> paths, bool queue = true)
    {
        var all = paths.ToArray();
        var files = all.Where(TrackInfo.IsAudioFile).ToArray();
        var otherFiles = all.Count(path => !TrackInfo.IsAudioFile(path) && File.Exists(path));
        if (otherFiles > 0) Log.Info($"Ignored {otherFiles} files that are not audio");
        if (files.Length == 0) return;

        Log.Info(queue ? $"Queueing {files.Length} files" : $"Adding {files.Length} files to the library");
        Task.Run(() => files.Select(TrackInfo.Read).ToArray()).DispatchMain(tracks =>
        {
            _library.Add(tracks);
            if (queue) _player.Add(tracks);
        });
    }

    private void PickFiles(bool queue = true)
    {
        Task.Run(() => IApplication.Get().SelectFile("Select audio files", true, "*.mp3;*.wav;*.flac;*.ogg"))
            .DispatchMain(paths => AddFiles(paths, queue));
    }

    private void ShowBackdrop()
    {
        if (_backdropCover is not null) _backdropCover.Changed -= ShowBackdropCover;
        _backdropCover = _player.Current?.Cover;
        _backdrop.Visibility = Visibility.Hidden;
        if (_backdropCover is null) return;

        _backdropCover.Changed += ShowBackdropCover;
        if (_backdropCover.HasImage) ShowBackdropCover();
    }

    private void ShowBackdropCover()
    {
        if (_backdropCover is null) return;

        _backdrop.Image = _backdropCover.Image;
        _backdrop.Visibility = Visibility.Visible;
    }

    public override void Dispose()
    {
        _player.TrackChanged -= ShowBackdrop;
        if (_backdropCover is not null) _backdropCover.Changed -= ShowBackdropCover;
        base.Dispose();
        _analyzer.Dispose();
        _equalizer.Dispose();
        _player.Dispose();
    }

    private static PanelSlot Fill(IView child)
    {
        return new PanelSlot { Child = child, MinAnchor = Vector2.Zero, MaxAnchor = Vector2.One };
    }
}
