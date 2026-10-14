using System.Numerics;
using Examples.AudioPlayer.Playback;
using Rin.Core.Animation;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.AudioPlayer.Views;

/// <summary>
///     A square that shows a track's cover, or a tinted placeholder with a note until the cover arrives.
/// </summary>
public sealed class CoverArtView : PanelView
{
    private const float FadeSeconds = 0.35f;

    private readonly RectView _placeholder;
    private readonly FontIconView _note;
    private readonly CoverImageView _image;
    private QueueEntry? _entry;

    public CoverArtView(float size, float radius)
    {
        _placeholder = new RectView { Color = PlaceholderColor(string.Empty), BorderRadius = BorderRadius.All(radius) };
        _note = new FontIconView
        {
            FontFamily = Icons.Family,
            Icon = Icons.Music,
            IconSize = size * 0.3f,
            Color = Color.White with { A = 0.35f }
        };
        _image = new CoverImageView { BorderRadius = BorderRadius.All(radius), Visibility = Visibility.Hidden };

        InitSlots =
        [
            Fill(_placeholder),
            new PanelSlot
            {
                Child = _note,
                MinAnchor = new Vector2(0.5f),
                MaxAnchor = new Vector2(0.5f),
                Alignment = new Vector2(0.5f),
                SizeToContent = true
            },
            Fill(_image)
        ];
    }

    public void Show(QueueEntry? entry)
    {
        if (_entry is not null) _entry.CoverChanged -= ShowCover;
        _entry = entry;
        _placeholder.Color = PlaceholderColor(entry?.Info.Title ?? string.Empty);
        _image.Visibility = Visibility.Hidden;
        if (entry is null) return;

        entry.CoverChanged += ShowCover;
        if (entry.HasCover) ShowCover();
    }

    private void ShowCover()
    {
        if (_entry is null) return;

        _image.Image = _entry.Cover;
        _image.Visibility = Visibility.Visible;
        this.StopAll().Transition(0f, 1f, alpha => _image.Tint = Color.White with { A = alpha }, FadeSeconds);
    }

    public override void Dispose()
    {
        Show(null);
        base.Dispose();
    }

    private static PanelSlot Fill(IView child)
    {
        return new PanelSlot { Child = child, MinAnchor = Vector2.Zero, MaxAnchor = Vector2.One };
    }

    private static Color PlaceholderColor(string seed)
    {
        var hash = 0;
        foreach (var c in seed) hash = hash * 31 + c;
        return Color.FromHsv((hash & 0xFFFF) / 65535f, 0.4f, 0.38f);
    }
}
