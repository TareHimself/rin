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
///     A webm video fitted to the window under a bar with open, a URL field, play and the time.
/// </summary>
public sealed class VideoPlayerDemoView : PanelView
{
    private const float ButtonWidth = 130f;
    private const float UrlWidth = 360f;

    private readonly FitterView _stage = new() { FittingMode = FitMode.Contain, Clip = Clip.Bounds };
    private readonly TextBoxView _hint = Ui.Label("Open a webm file, or paste a URL and press Enter.", 18f, Ui.Muted);
    private readonly TextBoxView _time = Ui.Label(string.Empty, 15f, Ui.Muted);
    private readonly LineInputView _url = new() { Placeholder = "https://example.com/video.webm" };
    private readonly ActionButton _playButton;
    private VideoPlayerView? _video;
    private bool _shownPlaying;
    private int _shownSecond = -1;
    private string _shownState = string.Empty;

    public VideoPlayerDemoView()
    {
        _playButton = Ui.QuietButton("Play", TogglePlay);
        _url.Submitted += OpenUrl;
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
                Ui.Slot(new SizerView { WidthOverride = UrlWidth, InitChild = Ui.Input(_url) }),
                Ui.Slot(Ui.Gap(0f, 10f)),
                Ui.Slot(new SizerView { WidthOverride = ButtonWidth, InitChild = Ui.QuietButton("Open URL", OpenUrl) }),
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
        if ((int)_video.Position == _shownSecond && _video.IsPlaying == _shownPlaying && State() == _shownState) return;

        ShowTime();
    }

    private void PickVideo()
    {
        Task.Run(() => IApplication.Get().SelectFile("Select a webm video", filter: "*.webm"))
            .DispatchMain(files =>
            {
                if (files.Length > 0) Open(VideoPlayerView.FromFile(files[0]));
            });
    }

    private void OpenUrl()
    {
        if (!Uri.TryCreate(_url.Content.Trim(), UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https"))
        {
            _time.Content = "Enter an http or https URL";
            return;
        }

        Open(VideoPlayerView.FromUrl(url));
    }

    /// <summary>
    ///     Opens an http or https URL, or otherwise a file path.
    /// </summary>
    public void Open(string pathOrUrl)
    {
        if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var url) && url.Scheme is "http" or "https")
        {
            _url.Content = pathOrUrl;
            Open(VideoPlayerView.FromUrl(url));
        }
        else
        {
            Open(VideoPlayerView.FromFile(pathOrUrl));
        }
    }

    private void Open(VideoPlayerView video)
    {
        CloseVideo();
        _video = video;
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

    private string State()
    {
        if (_video is null) return "none";
        if (_video.HasFailed) return "failed";
        return _video.IsReady ? "ready" : "loading";
    }

    private void ShowTime()
    {
        _shownPlaying = _video?.IsPlaying ?? false;
        _shownSecond = (int)(_video?.Position ?? 0.0);
        _shownState = State();
        _playButton.SetText(_shownPlaying ? "Pause" : "Play");
        _time.Content = _shownState switch
        {
            "none" => "No video",
            "loading" => "Loading...",
            "failed" => $"Failed: {_video!.Error?.Message}",
            _ => $"{FormatTime(_video!.Position)} / {FormatTime(_video.Duration)}"
        };
    }

    private static string FormatTime(double seconds)
    {
        var whole = (int)double.Max(0.0, seconds);
        return $"{whole / 60:00}:{whole % 60:00}";
    }
}
