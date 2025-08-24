using ManagedBass;

namespace Rin.Framework.Audio.BassAudio;

public class BassPushStream : BassChannel, IPushStream
{
    public BassPushStream(int handle) : base(handle)
    {
    }

    // public static BassStreamChannel FromFile(string filePath)
    // {
    //     return new BassStreamChannel(Bass.CreateStream(filePath));
    // }

    // public static async Task<BassStreamChannel> FromStream(Stream stream)
    // {
    //     Bass.CreateStream(0,0,(BassFlags)0,FileProcedures)
    //     var newStream = await stream.ReadAllAsync();
    //     return new BassStreamChannel(Bass.CreateStream(newStream, 0, newStream.Length, 0));
    // }


    public void Push(in ReadOnlySpan<byte> data)
    {
        unsafe
        {
            fixed (byte* ptr = data)
            {
                Bass.StreamPutData(_handle, new IntPtr(ptr), data.Length);
            }
        }
    }
}