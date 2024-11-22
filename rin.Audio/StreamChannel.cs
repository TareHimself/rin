using ManagedBass;
using rin.Core;

namespace rin.Audio;

public class StreamChannel : Channel
{
    public StreamChannel(int handle) : base(handle)
    {
    }
    
    public static StreamChannel FromFile(string filePath)
    {
        return new StreamChannel(Bass.CreateStream(filePath));
    }
}