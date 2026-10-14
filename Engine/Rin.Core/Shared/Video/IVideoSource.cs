namespace Rin.Core.Shared.Video;

/// <summary>
///     A webm file that may only be partly there. The decoder reads only what <see cref="AvailableFrom" /> reports as
///     held, and asks for the rest through <see cref="MakeRangeAvailable" />. Called from the decode thread only.
/// </summary>
public interface IVideoSource : IDisposable
{
    /// <summary>
    ///     Total size in bytes, or -1 while unknown. The source has ended once <see cref="Available" /> reaches it.
    /// </summary>
    public long Length { get; }

    /// <summary>
    ///     End of the bytes held from the start of the file, counting any released since. Never shrinks.
    /// </summary>
    public long Available { get; }

    public void Read(long offset, Span<byte> destination);

    /// <summary>
    ///     End of the unbroken run of held bytes starting at offset, or offset when that byte is not held.
    /// </summary>
    public long AvailableFrom(long offset)
    {
        return offset < Available ? Available : offset;
    }

    /// <summary>
    ///     The decoder needs bytes [offset, offset + size). Fetch them, or more, if the source can.
    /// </summary>
    public void MakeRangeAvailable(long offset, long size)
    {
    }

    /// <summary>
    ///     The decoder will not read below offset again unless it seeks back. Dropping those bytes is up to the source.
    /// </summary>
    public void ReleaseBefore(long offset)
    {
    }
}
