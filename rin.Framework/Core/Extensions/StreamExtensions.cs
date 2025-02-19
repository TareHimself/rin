namespace rin.Framework.Core.Extensions;

public static class StreamExtensions
{
    public static byte[] ReadAll(this Stream stream)
    {
        var result = new byte[stream.Length];
        stream.ReadExactly(result, 0, (int)(stream.Length - stream.Position));
        return result;
    }

    public static async Task<byte[]> ReadAllAsync(this Stream stream)
    {
        var result = new byte[stream.Length];
        await stream.ReadExactlyAsync(result, 0, (int)(stream.Length - stream.Position));
        return result;
    }
}