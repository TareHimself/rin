using System.Text.Json;
using Examples.Common;
using Rin.Core.Graphics;

namespace Examples.AudioPlayer.Library;

/// <summary>
///     Finds cover art for a track: the picture embedded in its tags, then an image next to the file,
///     then the iTunes Search API.
/// </summary>
public static class CoverFinder
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly string[] FolderArtNames = ["cover", "folder", "front", "album", "artwork"];
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png"];
    private static readonly Log Log = Log.For("CoverFinder");

    public static async Task<IHostImage?> Find(TrackInfo track, CancellationToken cancellation)
    {
        if (track.EmbeddedCover is { } embedded && Decode(embedded) is { } fromTags)
        {
            Log.Info($"Cover for '{track.Title}' from its tags");
            return fromTags;
        }

        if (FindFolderArt(track.Path) is { } fromFolder) return fromFolder;

        var fromItunes = await SearchItunes(track, cancellation);
        Log.Info(fromItunes is null ? $"No cover found for '{track.Title}'" : $"Cover for '{track.Title}' from iTunes");
        return fromItunes;
    }

    private static IHostImage? FindFolderArt(string trackPath)
    {
        var folder = Path.GetDirectoryName(trackPath);
        if (folder is null) return null;

        var trackName = Path.GetFileNameWithoutExtension(trackPath);
        foreach (var name in FolderArtNames.Prepend(trackName))
        foreach (var extension in ImageExtensions)
        {
            var candidate = Path.Combine(folder, name + extension);
            if (!File.Exists(candidate) || Decode(File.ReadAllBytes(candidate)) is not { } image) continue;

            Log.Info($"Cover for '{trackName}' from '{candidate}'");
            return image;
        }

        return null;
    }

    private static async Task<IHostImage?> SearchItunes(TrackInfo track, CancellationToken cancellation)
    {
        var term = $"{track.Artist} {track.Title}".Trim();
        if (term.Length == 0) return null;

        try
        {
            var url = $"https://itunes.apple.com/search?term={Uri.EscapeDataString(term)}&media=music&entity=song&limit=1";
            using var document = JsonDocument.Parse(await Http.GetStringAsync(url, cancellation));
            var results = document.RootElement.GetProperty("results");
            if (results.GetArrayLength() == 0) return null;

            var artwork = results[0].GetProperty("artworkUrl100").GetString();
            if (artwork is null) return null;

            var bytes = await Http.GetByteArrayAsync(artwork.Replace("100x100", "600x600"), cancellation);
            return Decode(bytes);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            if (!cancellation.IsCancellationRequested) Log.Warn($"iTunes lookup for '{term}' failed: {e.Message}");
            return null;
        }
    }

    private static IHostImage? Decode(byte[] bytes)
    {
        try
        {
            return HostImage.Create(bytes);
        }
        catch (Exception e)
        {
            Log.Warn($"Could not decode a cover image of {bytes.Length} bytes: {e.Message}");
            return null;
        }
    }
}
