using Examples.Common;

namespace Examples.AudioPlayer.Library;

/// <summary>
///     What the player shows for a file: its tags, or the "Artist - Title" split of its name when it has none.
/// </summary>
public sealed record TrackInfo(
    string Path,
    string Title,
    string Artist,
    string Album,
    double Duration,
    byte[]? EmbeddedCover)
{
    public static readonly string[] Extensions = [".mp3", ".wav", ".flac", ".ogg"];
    private static readonly Log Log = Log.For<TrackInfo>();

    public static bool IsAudioFile(string path)
    {
        return Extensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    public static TrackInfo Read(string path)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        var (fileArtist, fileTitle) = SplitFileName(fileName);

        ATL.Track tags;
        try
        {
            tags = new ATL.Track(path);
        }
        catch (Exception e)
        {
            Log.Warn($"Could not read the tags of '{path}', using its name: {e.Message}");
            return new TrackInfo(path, fileTitle, fileArtist, string.Empty, 0.0, null);
        }

        var title = string.IsNullOrWhiteSpace(tags.Title) || tags.Title == fileName ? fileTitle : tags.Title.Trim();
        var artist = string.IsNullOrWhiteSpace(tags.Artist) ? fileArtist : tags.Artist.Trim();
        var album = string.IsNullOrWhiteSpace(tags.Album) ? string.Empty : tags.Album.Trim();
        var cover = tags.EmbeddedPictures.FirstOrDefault(p => p.PictureData is { Length: > 0 })?.PictureData;

        return new TrackInfo(path, title, artist, album, tags.DurationMs / 1000.0, cover);
    }

    private static (string Artist, string Title) SplitFileName(string fileName)
    {
        var separator = fileName.IndexOf(" - ", StringComparison.Ordinal);
        if (separator <= 0) return (string.Empty, fileName.Trim());

        return (fileName[..separator].Trim(), fileName[(separator + 3)..].Trim());
    }
}
