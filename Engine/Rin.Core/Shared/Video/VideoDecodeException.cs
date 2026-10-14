namespace Rin.Core.Shared.Video;

/// <summary>
///     The decoder rejected the source: not webm, headers that never end, or invalid data after them.
/// </summary>
public sealed class VideoDecodeException(string message) : Exception(message);
