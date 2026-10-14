namespace Rin.Core.Shared.Video;

/// <summary>
///     Bytes [Start, End) of a source.
/// </summary>
public readonly record struct ByteRange(long Start, long End)
{
    public static readonly ByteRange Empty = new(0, 0);

    public long Length => End - Start;
    public bool IsEmpty => End <= Start;
}
