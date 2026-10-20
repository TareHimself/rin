using Examples.AudioPlayer.Library;

namespace Examples.AudioPlayer.Playback;

/// <summary>
///     An album in the albums view and its cover, taken from the album's cover track.
/// </summary>
public sealed class AlbumEntry(AlbumInfo album) : IDisposable
{
    public AlbumInfo Album { get; } = album;

    public TrackCover Cover { get; } = new(album.CoverTrack);

    public void Dispose()
    {
        Cover.Dispose();
    }
}
