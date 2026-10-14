using System.Numerics;
using Examples.AudioPlayer.Playback;
using Examples.Common.Views;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Layouts;

namespace Examples.AudioPlayer.Views;

/// <summary>
///     One track in the queue: its number, title, artist and length. Clicking it plays the track.
/// </summary>
public sealed class QueueRowView : RectView
{
    private const float NumberWidth = 30f;
    private const float RemoveSize = 28f;
    private static readonly Color Hover = Color.White with { A = 0.06f };
    private static readonly Color Active = Ui.Accent with { A = 0.18f };
    private static readonly Color ActiveHover = Ui.Accent with { A = 0.26f };

    private readonly TextBoxView _number;
    private readonly TextBoxView _title;
    private readonly IconButton _remove;
    private bool _active;
    private bool _down;

    public QueueRowView(QueueEntry entry, int index, Action play, Action remove)
    {
        var info = entry.Info;
        _number = Ui.Label((index + 1).ToString(), 13f, Ui.Muted);
        _title = Ui.Label(info.Title, 15f);
        _remove = new IconButton(Icons.Remove, RemoveSize, 12f, Ui.Clear, Color.White with { A = 0.12f },
            Color.White with { A = 0.2f }, Ui.Muted);
        _remove.Clicked += remove;
        _remove.Visibility = Visibility.Hidden;
        Play = play;

        var subtitle = info.Artist.Length > 0 ? info.Artist : "Unknown artist";
        Color = Ui.Clear;
        BorderRadius = BorderRadius.All(10f);
        Padding = new Padding(12f, 8f);
        Clip = Clip.Bounds;
        SetChild(Ui.Row(
            Ui.Slot(new SizerView { WidthOverride = NumberWidth, InitChild = _number }, fit: CrossFit.Desired,
                align: CrossAlign.Center),
            Ui.Slot(Ui.Column(Ui.Slot(_title), Ui.Slot(Ui.Label(subtitle, 12f, Ui.Muted))), 1f, CrossFit.Desired,
                CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 12f)),
            Ui.Slot(Ui.Label(FormatTime(info.Duration), 13f, Ui.Muted), fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 8f)),
            Ui.Slot(_remove, fit: CrossFit.Desired, align: CrossAlign.Center)));
    }

    private Action Play { get; }

    public void SetActive(bool active)
    {
        _active = active;
        _number.ForegroundColor = active ? Ui.Accent : Ui.Muted;
        _title.ForegroundColor = active ? Ui.Accent : Ui.Text;
        Color = RestingColor();
    }

    public void SetIndex(int index)
    {
        _number.Content = (index + 1).ToString();
    }

    private Color RestingColor()
    {
        if (_active) return IsHovered ? ActiveHover : Active;
        return IsHovered ? Hover : Ui.Clear;
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        _down = true;
        e.Target = this;
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        if (!_down) return;

        _down = false;
        if (IsHovered) Play();
    }

    protected override void OnCursorEnter(CursorMoveSurfaceEvent e)
    {
        base.OnCursorEnter(e);
        Color = RestingColor();
        _remove.Visibility = Visibility.Visible;
    }

    protected override void OnCursorLeave()
    {
        base.OnCursorLeave();
        Color = RestingColor();
        _remove.Visibility = Visibility.Hidden;
    }

    public static string FormatTime(double seconds)
    {
        var whole = (int)double.Max(0.0, seconds);
        return $"{whole / 60}:{whole % 60:00}";
    }
}
