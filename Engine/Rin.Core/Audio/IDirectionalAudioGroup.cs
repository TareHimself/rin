using System.Numerics;

namespace Rin.Core.Audio;

public interface IDirectionalAudioGroup : IAudioGroup
{
    void SetListenerPose(Vector3 position, Vector3 forward, Vector3 right);
    IChannel PlayAtPosition(IAudioSample sample, Vector3 position);
}
