using Examples.AudioPlayer.Library;

namespace Examples.AudioPlayer.Playback;

/// <summary>
///     Every track added since the player started, once per file, grouped into albums.
/// </summary>
public sealed class MusicLibrary
{
    private readonly Dictionary<string, TrackInfo> _tracks = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<AlbumInfo> Albums { get; private set; } = [];

    public int TrackCount => _tracks.Count;

    public event Action? Changed;

    public void Add(IReadOnlyList<TrackInfo> tracks)
    {
        var added = false;
        foreach (var track in tracks) added |= _tracks.TryAdd(track.Path, track);
        if (!added) return;

        Albums = AlbumInfo.Group(_tracks.Values);
        Changed?.Invoke();
    }
}
