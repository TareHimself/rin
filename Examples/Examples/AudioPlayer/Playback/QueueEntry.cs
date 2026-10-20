using Examples.AudioPlayer.Library;

namespace Examples.AudioPlayer.Playback;

/// <summary>
///     A track in the queue and its cover, which loads in the background after the entry is made.
/// </summary>
public sealed class QueueEntry(TrackInfo info) : IDisposable
{
    public TrackInfo Info { get; } = info;

    public TrackCover Cover { get; } = new(info);

    public void Dispose()
    {
        Cover.Dispose();
    }
}
