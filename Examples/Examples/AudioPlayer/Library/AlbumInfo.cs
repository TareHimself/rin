namespace Examples.AudioPlayer.Library;

/// <summary>
///     The tracks that share an album tag, in disc and track order, with what the album view shows about them.
/// </summary>
public sealed class AlbumInfo
{
    public const string UnknownAlbum = "Unknown Album";
    public const string UnknownArtist = "Unknown Artist";

    private AlbumInfo(string title, string artist, IReadOnlyList<TrackInfo> tracks)
    {
        Title = title;
        Artist = artist;
        Tracks = tracks;
        Year = tracks.Select(t => t.Year).FirstOrDefault(y => y > 0);
        Duration = tracks.Sum(t => t.Duration);
        CoverTrack = tracks.FirstOrDefault(t => t.EmbeddedCover is not null) ?? tracks[0];
    }

    public string Title { get; }

    public string Artist { get; }

    public int Year { get; }

    public IReadOnlyList<TrackInfo> Tracks { get; }

    public double Duration { get; }

    /// <summary>
    ///     The track whose cover stands for the album: the first with a picture in its tags, else the first track.
    /// </summary>
    public TrackInfo CoverTrack { get; }

    public int IndexOf(TrackInfo track)
    {
        for (var i = 0; i < Tracks.Count; i++)
            if (Tracks[i].Path == track.Path)
                return i;

        return -1;
    }

    /// <summary>
    ///     Groups tracks by album title, so featured artists on single tracks do not split an album. Tracks with no
    ///     album tag are grouped by artist instead, under "Unknown Album". Each album's tracks are ordered by disc
    ///     and track number, and the albums by artist then title.
    /// </summary>
    public static IReadOnlyList<AlbumInfo> Group(IEnumerable<TrackInfo> tracks)
    {
        return tracks
            .GroupBy(KeyOf)
            .Select(group => new AlbumInfo(group.Key.Title, ArtistOf(group), group.OrderBy(t => t.DiscNumber)
                .ThenBy(t => t.TrackNumber).ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase).ToArray()))
            .OrderBy(a => a.Artist, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static (string Title, string Artist) KeyOf(TrackInfo track)
    {
        return track.Album.Length > 0 ? (track.Album, string.Empty) : (UnknownAlbum, TrackArtistOf(track));
    }

    /// <summary>
    ///     The album artist tag when any track has one, else the artist credited on most tracks.
    /// </summary>
    private static string ArtistOf(IEnumerable<TrackInfo> tracks)
    {
        var all = tracks.ToArray();
        var albumArtist = all.Select(t => t.AlbumArtist).FirstOrDefault(a => a.Length > 0);
        if (albumArtist is not null) return albumArtist;

        return all.Select(TrackArtistOf)
            .GroupBy(a => a, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .First().Key;
    }

    private static string TrackArtistOf(TrackInfo track)
    {
        return track.Artist.Length > 0 ? track.Artist : UnknownArtist;
    }
}
