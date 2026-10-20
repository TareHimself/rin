using Examples.AudioPlayer.Library;
using Examples.AudioPlayer.Playback;

namespace Examples.Tests.AudioPlayer.Playback;

public class MusicLibraryTests
{
    private static TrackInfo Track(string path, string album)
    {
        return new TrackInfo(path, Path.GetFileNameWithoutExtension(path), "Ann", album, 60.0, null);
    }

    [Test]
    public void AddingTracksBuildsAlbumsAndRaisesChanged()
    {
        var library = new MusicLibrary();
        var changes = 0;
        library.Changed += () => changes++;

        library.Add([Track(@"C:\a\1.mp3", "Blue"), Track(@"C:\a\2.mp3", "Blue"), Track(@"C:\b\1.mp3", "Red")]);

        Assert.Multiple(() =>
        {
            Assert.That(library.TrackCount, Is.EqualTo(3));
            Assert.That(library.Albums.Select(a => a.Title), Is.EqualTo(new[] { "Blue", "Red" }));
            Assert.That(changes, Is.EqualTo(1));
        });
    }

    [Test]
    public void TheSameFileIsCountedOnceAndChangesNothing()
    {
        var library = new MusicLibrary();
        library.Add([Track(@"C:\a\1.mp3", "Blue")]);
        var changes = 0;
        library.Changed += () => changes++;

        library.Add([Track(@"C:\A\1.MP3", "Blue")]);

        Assert.Multiple(() =>
        {
            Assert.That(library.TrackCount, Is.EqualTo(1));
            Assert.That(changes, Is.Zero);
        });
    }
}
