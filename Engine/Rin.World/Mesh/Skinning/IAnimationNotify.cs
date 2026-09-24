using Rin.World.Components;
using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Mesh.Skinning;

public interface IAnimationNotify : IPooledNotify
{
    void Notify(SkinnedMeshComponent meshComponent, AnimationGraph graph);
}
