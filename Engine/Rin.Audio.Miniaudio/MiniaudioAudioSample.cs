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
        new MiniaudioActiveAudio(Native.SampleMakeActive(_id));

    public void Dispose() =>
        Native.audioSampleDispose(_id);

    public static MiniaudioAudioSample FromFile(string filePath) =>
        new(Native.MakeSampleFromFile(filePath));

    public static unsafe MiniaudioAudioSample FromMemory(ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
            return new(Native.MakeSampleFromMemory(ptr, (nuint)data.Length));
    }

    public static MiniaudioAudioSample StreamFromFile(string filePath) =>
        new(Native.MakeStreamFromFile(filePath));

    public static unsafe MiniaudioAudioSample StreamFromMemory(ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
            return new(Native.MakeStreamFromMemory(ptr, (nuint)data.Length));
    }
}
