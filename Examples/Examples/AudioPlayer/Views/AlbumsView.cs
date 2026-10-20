using System.Numerics;
using Examples.AudioPlayer.Library;
using Examples.AudioPlayer.Playback;
using Examples.Common.Views;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Layouts;

namespace Examples.AudioPlayer.Views;

/// <summary>
///     The albums card: a grid of every album in the library, and the open album's tracks with Play and
///     Add to queue.
/// </summary>
public sealed class AlbumsView : CardView
{
    private const float DetailCover = 160f;
    private const float DetailRadius = 14f;
    private const float NumberWidth = 30f;

    private readonly MusicLibrary _library;
    private readonly Player _player;
    private readonly TextBoxView _title = Ui.Label("Albums", 20f);
    private readonly TextBoxView _count = Ui.Label(string.Empty, 14f, Ui.Muted);
    private readonly TextBoxView _hint = Ui.Label("Add tracks with the button above, or drop them on the window, to see their albums here.", 15f, Ui.Muted);
    private readonly IconButton _back;
    private readonly WrapListView _grid = new() { Axis = Axis.Row };
    private readonly RectView _detail = new() { Color = Ui.Clear };
    private readonly SwitcherView _pages = new();
    private readonly List<AlbumEntry> _entries = [];
    private readonly List<AlbumTrackRowView> _rows = [];
    private AlbumEntry? _open;

    public AlbumsView(MusicLibrary library, Player player, Action addFiles)
    {
        _library = library;
        _player = player;
        Color = PlayerView.Glass;
        BorderColor = PlayerView.GlassBorder;
        BorderRadius = BorderRadius.All(22f);
        Padding = new Padding(20f);

        _back = new IconButton(Icons.Back, 36f, 13f, Ui.Clear, Color.White with { A = 0.12f },
            Color.White with { A = 0.2f }, Ui.Text) { Visibility = Visibility.Collapsed };
        _back.Clicked += ShowGrid;
        var add = new IconButton(Icons.Plus, 36f, 14f, Ui.Accent, Ui.AccentHover, Ui.AccentPressed, Color.White);
        add.Clicked += addFiles;

        _pages.Add(new Slot
        {
            Child = new PanelView
            {
                InitSlots =
                [
                    new PanelSlot
                    {
                        Child = new ScrollListView
                        {
                            Axis = Axis.Column,
                            BarColor = Color.White with { A = 0.25f },
                            InitSlots = [new ListSlot { Child = _grid, Fit = CrossFit.Available }]
                        },
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
                    }
                ]
            }
        });
        _pages.Add(new Slot { Child = _detail });

        SetChild(Ui.Column(
            Ui.Slot(Ui.Row(
                Ui.Slot(_back, fit: CrossFit.Desired, align: CrossAlign.Center),
                Ui.Slot(_title, fit: CrossFit.Desired, align: CrossAlign.Center),
                Ui.Slot(Ui.Gap(0f, 10f)),
                Ui.Slot(_count, 1f, CrossFit.Desired, CrossAlign.Center),
                Ui.Slot(add, fit: CrossFit.Desired, align: CrossAlign.Center))),
            Ui.Slot(Ui.Gap(14f)),
            Ui.Slot(_pages, 1f)));

        library.Changed += Rebuild;
        player.TrackChanged += Highlight;
        Rebuild();
    }

    private void Rebuild()
    {
        var openTitle = _open?.Album.Title;
        var openArtist = _open?.Album.Artist;

        foreach (var child in _grid.GetChildren())
        {
            _grid.Remove(child);
            child.Dispose();
        }

        foreach (var entry in _entries) entry.Dispose();
        _entries.Clear();
        _open = null;

        foreach (var album in _library.Albums)
        {
            var entry = new AlbumEntry(album);
            _entries.Add(entry);
            _grid.Add(new ListSlot { Child = new AlbumTileView(entry, () => ShowAlbum(entry)) });
        }

        _hint.Visibility = _entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        _count.Content = _entries.Count switch
        {
            0 => string.Empty,
            1 => "1 album",
            var n => $"{n} albums"
        };

        var reopened = _entries.FirstOrDefault(e => e.Album.Title == openTitle && e.Album.Artist == openArtist);
        if (reopened is not null) ShowAlbum(reopened);
        else ShowGrid();
    }

    private void ShowGrid()
    {
        _open = null;
        _detail.GetChild()?.Dispose();
        _detail.SetChild(null);
        _rows.Clear();
        _title.Content = "Albums";
        _back.Visibility = Visibility.Collapsed;
        _count.Visibility = Visibility.Visible;
        _pages.SelectedIndex = 0;
    }

    private void ShowAlbum(AlbumEntry entry)
    {
        _open = entry;
        var album = entry.Album;
        _title.Content = album.Title;
        _back.Visibility = Visibility.Visible;
        _count.Visibility = Visibility.Collapsed;

        var cover = new CoverArtView(DetailCover, DetailRadius);
        cover.Show(entry.Cover, album.Title);
        var play = Ui.PrimaryButton("Play", () => _player.Replace(album.Tracks));
        var queue = Ui.QuietButton("Add to queue", () => _player.Add(album.Tracks));

        var list = new ScrollListView { Axis = Axis.Column, BarColor = Color.White with { A = 0.25f } };
        _rows.Clear();
        for (var i = 0; i < album.Tracks.Count; i++)
        {
            var index = i;
            var row = new AlbumTrackRowView(album.Tracks[i], i, () => _player.Replace(album.Tracks, index));
            _rows.Add(row);
            list.Add(new ListSlot { Child = row, Fit = CrossFit.Fill });
        }

        _detail.GetChild()?.Dispose();
        _detail.SetChild(Ui.Column(
            Ui.Slot(Ui.Row(
                Ui.Slot(new SizerView { WidthOverride = DetailCover, HeightOverride = DetailCover, InitChild = cover },
                    fit: CrossFit.Desired),
                Ui.Slot(Ui.Gap(0f, 18f)),
                Ui.Slot(Ui.Column(
                    Ui.Slot(Ui.Label(album.Artist, 15f)),
                    Ui.Slot(Ui.Gap(4f)),
                    Ui.Slot(Ui.Label(Summary(album), 13f, Ui.Muted)),
                    Ui.Slot(Ui.Gap(14f)),
                    Ui.Slot(Ui.Row(
                        Ui.Slot(play, fit: CrossFit.Desired),
                        Ui.Slot(Ui.Gap(0f, 8f)),
                        Ui.Slot(queue, fit: CrossFit.Desired)))), 1f, CrossFit.Desired, CrossAlign.Center))),
            Ui.Slot(Ui.Gap(16f)),
            Ui.Slot(list, 1f)));
        _pages.SelectedIndex = 1;
        Highlight();
    }

    private static string Summary(AlbumInfo album)
    {
        var tracks = album.Tracks.Count == 1 ? "1 track" : $"{album.Tracks.Count} tracks";
        var length = QueueRowView.FormatTime(album.Duration);
        return album.Year > 0 ? $"{album.Year}  ·  {tracks}  ·  {length}" : $"{tracks}  ·  {length}";
    }

    private void Highlight()
    {
        var current = _player.Current?.Info.Path;
        foreach (var row in _rows) row.SetActive(row.Track.Path == current);
    }

    public override void Dispose()
    {
        _library.Changed -= Rebuild;
        _player.TrackChanged -= Highlight;
        base.Dispose();
        foreach (var entry in _entries) entry.Dispose();
        _entries.Clear();
    }

    /// <summary>
    ///     One track of the open album: its number, title and length. Clicking it plays the album from there.
    /// </summary>
    private sealed class AlbumTrackRowView : RectView
    {
        private static readonly Color Hover = Color.White with { A = 0.06f };
        private static readonly Color Active = Ui.Accent with { A = 0.18f };
        private static readonly Color ActiveHover = Ui.Accent with { A = 0.26f };

        private readonly TextBoxView _number;
        private readonly TextBoxView _title;
        private readonly Action _play;
        private bool _active;
        private bool _down;

        public AlbumTrackRowView(TrackInfo track, int index, Action play)
        {
            Track = track;
            _play = play;
            _number = Ui.Label((track.TrackNumber > 0 ? track.TrackNumber : index + 1).ToString(), 13f, Ui.Muted);
            _title = Ui.Label(track.Title, 15f);
            Color = Ui.Clear;
            BorderRadius = BorderRadius.All(10f);
            Padding = new Padding(12f, 8f);
            SetChild(Ui.Row(
                Ui.Slot(new SizerView { WidthOverride = NumberWidth, InitChild = _number }, fit: CrossFit.Desired,
                    align: CrossAlign.Center),
                Ui.Slot(_title, 1f, CrossFit.Desired, CrossAlign.Center),
                Ui.Slot(Ui.Gap(0f, 12f)),
                Ui.Slot(Ui.Label(QueueRowView.FormatTime(track.Duration), 13f, Ui.Muted), fit: CrossFit.Desired,
                    align: CrossAlign.Center)));
        }

        public TrackInfo Track { get; }

        public void SetActive(bool active)
        {
            _active = active;
            _number.ForegroundColor = active ? Ui.Accent : Ui.Muted;
            _title.ForegroundColor = active ? Ui.Accent : Ui.Text;
            Color = RestingColor();
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
            if (IsHovered) _play();
        }

        protected override void OnCursorEnter(CursorMoveSurfaceEvent e)
        {
            base.OnCursorEnter(e);
            Color = RestingColor();
        }

        protected override void OnCursorLeave()
        {
            base.OnCursorLeave();
            Color = RestingColor();
        }
    }
}
