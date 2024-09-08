using ManagedBass;

namespace aerox.Runtime.Audio;

public class AudioStream : Channel
{
    public AudioStream(int handle) : base(handle)
    {
        
    }

    public static AudioStream FromFile(string filePath)
    {
        return new AudioStream(Bass.CreateStream(filePath));
    }

    
    
    
}