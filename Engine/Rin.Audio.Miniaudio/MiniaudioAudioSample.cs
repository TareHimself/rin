using Rin.Core.Audio;

namespace Rin.Audio.Miniaudio;

public class MiniaudioAudioSample : IAudioSample
{
    private readonly ulong _id;

    internal MiniaudioAudioSample(ulong id)
    {
        _id = id;
    }

    internal ulong NativeId => _id;

    public IActiveAudio MakeActive() =>
        new MiniaudioActiveAudio(Native.audioSampleMakeActive(_id));

    public void Dispose() =>
        Native.audioSampleDispose(_id);

    public static MiniaudioAudioSample FromFile(string filePath) =>
        new(Native.audioMakeSampleFromFile(filePath));

    public static unsafe MiniaudioAudioSample FromMemory(ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
            return new(Native.audioMakeSampleFromMemory(ptr, (nuint)data.Length));
    }

    public static MiniaudioAudioSample StreamFromFile(string filePath) =>
        new(Native.audioMakeStreamFromFile(filePath));

    public static unsafe MiniaudioAudioSample StreamFromMemory(ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
            return new(Native.audioMakeStreamFromMemory(ptr, (nuint)data.Length));
    }
}
