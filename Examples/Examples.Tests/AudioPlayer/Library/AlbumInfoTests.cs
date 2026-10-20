using Examples.AudioPlayer.Library;

namespace Examples.Tests.AudioPlayer.Library;

public class AlbumInfoTests
{
    private static TrackInfo Track(string title, string artist = "Ann", string album = "Blue", int track = 0,
        int disc = 0, int year = 0, string albumArtist = "", double duration = 60.0, byte[]? cover = null)
    {
        return new TrackInfo($@"C:\music\{album}\{title}.mp3", title, artist, album, duration, cover, albumArtist, disc,
            track, year);
    }

    [Test]
    public void TracksAreGroupedByAlbumAndOrderedByDiscThenTrack()
    {
        var albums = AlbumInfo.Group([
            Track("Three", track: 3),
            Track("One", track: 1),
            Track("Disc two", track: 1, disc: 2),
            Track("Other", album: "Red", track: 1)
        ]);

        Assert.That(albums.Select(a => a.Title), Is.EqualTo(new[] { "Blue", "Red" }));
        Assert.That(albums[0].Tracks.Select(t => t.Title), Is.EqualTo(new[] { "One", "Three", "Disc two" }));
    }

    [Test]
    public void UntaggedTracksFallBackToTitleOrder()
    {
        var albums = AlbumInfo.Group([Track("b"), Track("a"), Track("C")]);

        Assert.That(albums[0].Tracks.Select(t => t.Title), Is.EqualTo(new[] { "a", "b", "C" }));
    }

    [Test]
    public void FeaturedArtistsDoNotSplitAnAlbum()
    {
        var album = AlbumInfo.Group([
            Track("One", artist: "Band", track: 1),
            Track("Two", artist: "Band feat. Guest", track: 2),
            Track("Three", artist: "Band", track: 3)
        ]).Single();

        Assert.Multiple(() =>
        {
            Assert.That(album.Tracks, Has.Count.EqualTo(3));
            Assert.That(album.Artist, Is.EqualTo("Band"));
        });
    }

    [Test]
    public void AlbumArtistTagNamesTheAlbum()
    {
        var album = AlbumInfo.Group([
            Track("Song", artist: "Guest", albumArtist: "Band"),
            Track("Other song", artist: "Guest")
        ]).Single();

        Assert.That(album.Artist, Is.EqualTo("Band"));
    }

    [Test]
    public void TracksWithoutAnAlbumAreGroupedByArtist()
    {
        var albums = AlbumInfo.Group([
            Track("a", artist: "Ann", album: ""),
            Track("b", artist: "Ann", album: ""),
            Track("c", artist: "Zed", album: ""),
            Track("d", artist: "", album: "")
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(albums.Select(a => a.Title), Is.All.EqualTo(AlbumInfo.UnknownAlbum));
            Assert.That(albums.Select(a => a.Artist), Is.EqualTo(new[] { "Ann", AlbumInfo.UnknownArtist, "Zed" }));
            Assert.That(albums[0].Tracks, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void AlbumsAreOrderedByArtistThenTitle()
    {
        var albums = AlbumInfo.Group([
            Track("x", artist: "zoe", album: "Alpha"),
            Track("y", artist: "amy", album: "Zulu"),
            Track("z", artist: "amy", album: "Beta")
        ]);

        Assert.That(albums.Select(a => $"{a.Artist}/{a.Title}"), Is.EqualTo(new[] { "amy/Beta", "amy/Zulu", "zoe/Alpha" }));
    }

    [Test]
    public void YearDurationAndCoverTrackComeFromTheTracks()
    {
        var withCover = Track("Two", track: 2, cover: [1, 2, 3]);
        var album = AlbumInfo.Group([Track("One", track: 1, duration: 100.0), withCover, Track("Three", track: 3, year: 2021)]).Single();

        Assert.Multiple(() =>
        {
            Assert.That(album.Year, Is.EqualTo(2021));
            Assert.That(album.Duration, Is.EqualTo(220.0));
            Assert.That(album.CoverTrack, Is.SameAs(withCover));
            Assert.That(album.IndexOf(withCover), Is.EqualTo(1));
        });
    }
}
