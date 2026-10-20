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
///     One album in the grid: its cover, title and artist. Clicking it opens the album.
/// </summary>
public sealed class AlbumTileView : RectView
{
    public const float Width = 172f;
    private const float CoverRadius = 14f;
    private static readonly Color Hover = Color.White with { A = 0.06f };

    private readonly CoverArtView _cover = new(Width, CoverRadius);
    private readonly Action _open;
    private bool _down;

    public AlbumTileView(AlbumEntry entry, Action open)
    {
        _open = open;
        var album = entry.Album;
        Color = Ui.Clear;
        BorderRadius = BorderRadius.All(CoverRadius + 8f);
        Padding = new Padding(8f);
        _cover.Show(entry.Cover, album.Title);

        SetChild(new SizerView
        {
            WidthOverride = Width,
            InitChild = Ui.Column(
                Ui.Slot(new SizerView { HeightOverride = Width, InitChild = _cover }),
                Ui.Slot(Ui.Gap(8f)),
                Ui.Slot(new RectView
                {
                    Color = Ui.Clear,
                    Clip = Clip.Bounds,
                    InitChild = new SizerView { WidthOverride = Width, HeightOverride = 22f, InitChild = Ui.Label(album.Title, 15f) }
                }),
                Ui.Slot(Ui.Gap(2f)),
                Ui.Slot(new RectView
                {
                    Color = Ui.Clear,
                    Clip = Clip.Bounds,
                    InitChild = new SizerView { WidthOverride = Width, HeightOverride = 18f, InitChild = Ui.Label(album.Artist, 13f, Ui.Muted) }
                }))
        });
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
        if (IsHovered) _open();
    }

    protected override void OnCursorEnter(CursorMoveSurfaceEvent e)
    {
        base.OnCursorEnter(e);
        Color = Hover;
    }

    protected override void OnCursorLeave()
    {
        base.OnCursorLeave();
        Color = Ui.Clear;
    }
}
