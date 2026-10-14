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
///     The whole player: the current cover blurred across the window, the now playing column and the queue.
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
    private readonly Equalizer _equalizer;
    private readonly SpectrumAnalyzer _analyzer;
    private readonly CoverImageView _backdrop = new() { Tint = BackdropTint, Visibility = Visibility.Hidden };
    private QueueEntry? _backdropEntry;

    public PlayerView()
    {
        // Effects run in the order they were added, so the spectrum shows the equalized output.
        var master = IAudioModule.Get().MasterAudioGroup;
        _equalizer = new Equalizer(master);
        _analyzer = new SpectrumAnalyzer(master);

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
                            InitChild = new NowPlayingView(_player, _analyzer)
                        }, fit: CrossFit.Desired, align: CrossAlign.Center),
                    Ui.Slot(Ui.Gap(0f, ColumnGap)),
                    Ui.Slot(Ui.Column(
                        Ui.Slot(new QueueView(_player, PickFiles), 1f),
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

    /// <summary>
    ///     Reads the tags of every audio file among the paths on a worker and queues them in order.
    /// </summary>
    public void AddFiles(IEnumerable<string> paths)
    {
        var all = paths.ToArray();
        var files = all.Where(TrackInfo.IsAudioFile).ToArray();
        var otherFiles = all.Count(path => !TrackInfo.IsAudioFile(path) && File.Exists(path));
        if (otherFiles > 0) Log.Info($"Ignored {otherFiles} files that are not audio");
        if (files.Length == 0) return;

        Log.Info($"Queueing {files.Length} files");
        Task.Run(() => files.Select(TrackInfo.Read).ToArray()).DispatchMain(_player.Add);
    }

    private void PickFiles()
    {
        Task.Run(() => IApplication.Get().SelectFile("Select audio files", true, "*.mp3;*.wav;*.flac;*.ogg"))
            .DispatchMain(AddFiles);
    }

    private void ShowBackdrop()
    {
        if (_backdropEntry is not null) _backdropEntry.CoverChanged -= ShowBackdropCover;
        _backdropEntry = _player.Current;
        _backdrop.Visibility = Visibility.Hidden;
        if (_backdropEntry is null) return;

        _backdropEntry.CoverChanged += ShowBackdropCover;
        if (_backdropEntry.HasCover) ShowBackdropCover();
    }

    private void ShowBackdropCover()
    {
        if (_backdropEntry is null) return;

        _backdrop.Image = _backdropEntry.Cover;
        _backdrop.Visibility = Visibility.Visible;
    }

    public override void Dispose()
    {
        _player.TrackChanged -= ShowBackdrop;
        if (_backdropEntry is not null) _backdropEntry.CoverChanged -= ShowBackdropCover;
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
