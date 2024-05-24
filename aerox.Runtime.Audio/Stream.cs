using ManagedBass;

namespace aerox.Runtime.Audio;

public class Stream(int handle) : Channel(handle)
{
    public static Stream FromFile(string filePath)
    {
        return new Stream(Bass.CreateStream(filePath));
    }
}