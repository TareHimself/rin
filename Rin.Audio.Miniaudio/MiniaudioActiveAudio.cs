using Rin.Core.Audio;

namespace Rin.Audio.Miniaudio;

public class MiniaudioActiveAudio : IActiveAudio, IChannel
{
    protected readonly ulong Id;

    internal MiniaudioActiveAudio(ulong id)
    {
        Id = id;
    }

    public bool IsPlaying => Native.audioActiveIsPlaying(Id) != 0;

    public double Position => Native.audioActiveGetPosition(Id);
    public double Duration => Native.audioActiveGetDuration(Id);

    public bool Play(bool restart = false) =>
        Native.audioActivePlay(Id, restart ? 1 : 0) != 0;

    public bool Pause() =>
        Native.audioActivePause(Id) != 0;

    public bool SetVolume(float value) =>
        Native.audioActiveSetVolume(Id, value) != 0;

    public bool SetPosition(double position) =>
        Native.audioActiveSetPosition(Id, position) != 0;

    public void Dispose() =>
        Native.audioActiveDispose(Id);
}
