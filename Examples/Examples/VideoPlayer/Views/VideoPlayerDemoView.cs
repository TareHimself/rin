using System.Numerics;
using Examples.Common.Views;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.VideoPlayer.Views;

/// <summary>
///     A webm video fitted to the window under a bar with open, play and the time.
/// </summary>
public sealed class VideoPlayerDemoView : PanelView
{
    private const float ButtonWidth = 130f;

    private readonly FitterView _stage = new() { FittingMode = FitMode.Contain, Clip = Clip.Bounds };
    private readonly TextBoxView _hint = Ui.Label("Open a webm video to play it.", 18f, Ui.Muted);
    private readonly TextBoxView _time = Ui.Label(string.Empty, 15f, Ui.Muted);
    private readonly ActionButton _playButton;
    private VideoPlayerView? _video;
    private bool _shownPlaying;
    private int _shownSecond = -1;

    public VideoPlayerDemoView()
    {
        _playButton = Ui.QuietButton("Play", TogglePlay);
        ShowTime();

        InitSlots =
        [
            new PanelSlot
            {
                Child = new RectView { Color = Ui.Background },
                MinAnchor = Vector2.Zero,
                MaxAnchor = Vector2.One
            },
            new PanelSlot
            {
                Child = _stage,
                MinAnchor = Vector2.Zero,
                MaxAnchor = Vector2.One
            },
            new PanelSlot
            {
                Child = _hint,
                MinAnchor = new Vector2(0.5f),
                MaxAnchor = new Vector2(0.5f),
                Alignment = new Vector2(0.5f),
                SizeToContent = true
            },
            new PanelSlot
            {
                Child = BuildBar(),
                Offset = new Vector2(16f),
                SizeToContent = true
            }
        ];
    }

    private CardView BuildBar()
    {
        return new CardView
        {
            Color = Ui.Surface,
            BorderColor = Ui.Border,
            ShadowAlpha = 0.4f,
            BorderRadius = BorderRadius.All(16f),
            Padding = new Padding(12f),
            InitChild = Ui.Row(
                Ui.Slot(new SizerView { WidthOverride = ButtonWidth, InitChild = Ui.PrimaryButton("Open video", PickVideo) }),
                Ui.Slot(Ui.Gap(0f, 10f)),
                Ui.Slot(new SizerView { WidthOverride = ButtonWidth, InitChild = _playButton }),
                Ui.Slot(Ui.Gap(0f, 14f)),
                Ui.Slot(_time, fit: CrossFit.Desired, align: CrossAlign.Center))
        };
    }

    private void CloseVideo()
    {
        _stage.SetChild(null);
        _video?.Dispose();
        _video = null;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (_video is null) return;
        if ((int)_video.Position == _shownSecond && _video.IsPlaying == _shownPlaying) return;

        ShowTime();
    }

    private void PickVideo()
    {
        Task.Run(() => IApplication.Get().SelectFile("Select a webm video", filter: "*.webm"))
            .DispatchMain(files =>
            {
                if (files.Length > 0) Open(files[0]);
            });
    }

    private void Open(string file)
    {
        CloseVideo();
        _video = VideoPlayerView.FromFile(file);
        _stage.SetChild(_video);
        _hint.Visibility = Visibility.Hidden;
        _video.Play();
        ShowTime();
    }

    private void TogglePlay()
    {
        if (_video is null) return;

        if (_video.IsPlaying)
            _video.Pause();
        else
            _video.Play();
        ShowTime();
    }

    private void ShowTime()
    {
        _shownPlaying = _video?.IsPlaying ?? false;
        _shownSecond = (int)(_video?.Position ?? 0.0);
        _playButton.SetText(_shownPlaying ? "Pause" : "Play");
        _time.Content = _video is null
            ? "No video"
            : $"{FormatTime(_video.Position)} / {FormatTime(_video.Duration)}";
    }

    private static string FormatTime(double seconds)
    {
        var whole = (int)double.Max(0.0, seconds);
        return $"{whole / 60:00}:{whole % 60:00}";
    }
}
