using System.Numerics;

namespace Rin.Framework.Audio;

public interface IAudioScene
{
    public void SetListenerPose(Vector3 position, Vector3 forward, Vector3 right);
    public IChannel PlatAtLocation(IAudioSample audioSample,Vector3 location);
    public IChannel Play2d(IAudioSample audioSample);
}