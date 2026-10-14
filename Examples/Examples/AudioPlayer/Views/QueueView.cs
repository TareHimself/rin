using System.Numerics;
using Examples.AudioPlayer.Playback;
using Examples.Common.Views;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.AudioPlayer.Views;

/// <summary>
///     The queue card: a header with the track count and an add button over the scrolling list of tracks.
/// </summary>
public sealed class QueueView : CardView
{
    private readonly Player _player;
    private readonly TextBoxView _count = Ui.Label(string.Empty, 14f, Ui.Muted);
    private readonly TextBoxView _hint = Ui.Label("Drop audio files here, or add them with the button above.", 15f, Ui.Muted);
    private readonly ScrollListView _list = new() { Axis = Axis.Column, BarColor = Color.White with { A = 0.25f } };
    private readonly List<QueueRowView> _rows = [];

    public QueueView(Player player, Action addFiles)
    {
        _player = player;
        Color = PlayerView.Glass;
        BorderColor = PlayerView.GlassBorder;
        BorderRadius = BorderRadius.All(22f);
        Padding = new Padding(20f);

        var add = new IconButton(Icons.Plus, 36f, 14f, Ui.Accent, Ui.AccentHover, Ui.AccentPressed, Color.White);
        add.Clicked += addFiles;

        SetChild(Ui.Column(
            Ui.Slot(Ui.Row(
                Ui.Slot(Ui.Label("Queue", 20f), fit: CrossFit.Desired, align: CrossAlign.Center),
                Ui.Slot(Ui.Gap(0f, 10f)),
                Ui.Slot(_count, 1f, CrossFit.Desired, CrossAlign.Center),
                Ui.Slot(add, fit: CrossFit.Desired, align: CrossAlign.Center))),
            Ui.Slot(Ui.Gap(14f)),
            Ui.Slot(new PanelView
            {
                InitSlots =
                [
                    new PanelSlot { Child = _list, MinAnchor = Vector2.Zero, MaxAnchor = Vector2.One },
                    new PanelSlot
                    {
                        Child = _hint,
                        MinAnchor = new Vector2(0.5f),
                        MaxAnchor = new Vector2(0.5f),
                        Alignment = new Vector2(0.5f),
                        SizeToContent = true
                    }
                ]
            }, 1f)));

        player.QueueChanged += Rebuild;
        player.TrackChanged += Highlight;
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (var row in _rows)
        {
            _list.Remove(row);
            row.Dispose();
        }

        _rows.Clear();

        for (var i = 0; i < _player.Queue.Count; i++)
        {
            var index = i;
            var row = new QueueRowView(_player.Queue[i], i, () => _player.Play(index), () => _player.Remove(index));
            _rows.Add(row);
            _list.Add(new ListSlot { Child = row, Fit = CrossFit.Fill });
        }

        _hint.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        _count.Content = _rows.Count switch
        {
            0 => string.Empty,
            1 => "1 track",
            var n => $"{n} tracks"
        };
        Highlight();
    }

    private void Highlight()
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            _rows[i].SetIndex(i);
            _rows[i].SetActive(i == _player.CurrentIndex);
        }
    }

    public override void Dispose()
    {
        _player.QueueChanged -= Rebuild;
        _player.TrackChanged -= Highlight;
        base.Dispose();
    }
}
