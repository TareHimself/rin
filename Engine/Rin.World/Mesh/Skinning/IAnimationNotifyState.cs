using Rin.World.Components;
using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Mesh.Skinning;

public interface IAnimationNotifyState : IPooledNotify
{
    void NotifyBegin(SkinnedMeshComponent meshComponent, AnimationGraph graph);
    void NotifyEnd(SkinnedMeshComponent meshComponent, AnimationGraph graph);
}
