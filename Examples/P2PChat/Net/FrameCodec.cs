using System.Buffers.Binary;
using System.Text.Json;

namespace P2PChat.Net;

/// <summary>
///     Frames are a 4 byte big-endian length followed by that many bytes of UTF-8 JSON.
/// </summary>
public static class FrameCodec
{
    public const int MaxFrameBytes = 64 * 1024;

    public static async Task WriteAsync(Stream stream, Frame frame, CancellationToken cancellation)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(frame);
        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);
        await stream.WriteAsync(header, cancellation);
        await stream.WriteAsync(payload, cancellation);
        await stream.FlushAsync(cancellation);
    }

    public static async Task<Frame?> ReadAsync(Stream stream, CancellationToken cancellation)
    {
        var header = new byte[sizeof(int)];
        if (!await ReadExactAsync(stream, header, cancellation)) return null;

        var length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length is <= 0 or > MaxFrameBytes) throw new InvalidDataException($"Invalid frame length {length}");

        var payload = new byte[length];
        if (!await ReadExactAsync(stream, payload, cancellation)) return null;
        return JsonSerializer.Deserialize<Frame>(payload);
    }

    private static async Task<bool> ReadExactAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellation)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var count = await stream.ReadAsync(buffer[read..], cancellation);
            if (count == 0) return false;
            read += count;
        }

        return true;
    }
}
