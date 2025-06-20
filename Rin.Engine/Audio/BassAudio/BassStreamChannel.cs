using ManagedBass;
using Rin.Engine.Extensions;

namespace Rin.Engine.Audio.BassAudio;

public class BassStreamChannel : BassChannel, IStream
{
    public BassStreamChannel(int handle) : base(handle)
    {
    }

    public static BassStreamChannel FromFile(string filePath)
    {
        return new BassStreamChannel(Bass.CreateStream(filePath));
    }

    // public static async Task<BassStreamChannel> FromStream(Stream stream)
    // {
    //     Bass.CreateStream(0,0,(BassFlags)0,FileProcedures)
    //     var newStream = await stream.ReadAllAsync();
    //     return new BassStreamChannel(Bass.CreateStream(newStream, 0, newStream.Length, 0));
    // }
    
    
}