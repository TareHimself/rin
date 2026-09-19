using Rin.Core.Audio;

namespace Rin.Audio.Null;

internal class NullActiveAudio : IActiveAudio
{
    public bool IsPlaying => false;
    public double Position => 0;
    public double Duration => 0;

    public bool Play(bool restart = false)
    {
        return false;
    }

    public bool Pause()
    {
        return false;
    }

    public bool SetVolume(float value)
    {
        return false;
    }

    public bool SetPosition(double position)
    {
        return false;
    }

    public void Dispose()
    {
    }
}
