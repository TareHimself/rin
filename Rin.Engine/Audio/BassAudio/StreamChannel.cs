using ManagedBass;
using Rin.Engine.Extensions;

namespace Rin.Engine.Audio.BassAudio;

public class StreamChannel : BassChannel, IStream
{
    public StreamChannel(int handle) : base(handle)
    {
    }

    public static StreamChannel FromFile(string filePath)
    {
        return new StreamChannel(Bass.CreateStream(filePath));
    }

    public static async Task<StreamChannel> FromStream(Stream stream)
    {
        var newStream = await stream.ReadAllAsync();
        return new StreamChannel(Bass.CreateStream(newStream, 0, newStream.Length, 0));
    }
}