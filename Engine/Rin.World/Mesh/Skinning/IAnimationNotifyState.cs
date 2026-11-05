using Rin.World.Components;
using Rin.World.Ecs;
using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Mesh.Skinning;

public interface IAnimationNotifyState : IPooledNotify
{
    void NotifyBegin(SkinnedMeshComponent meshComponent, AnimationGraph graph)
    {
    }

    void NotifyEnd(SkinnedMeshComponent meshComponent, AnimationGraph graph)
    {
    }

    /// <summary>
    ///     Called after the graph advanced, outside any query loop, so structural changes are allowed.
    /// </summary>
    void NotifyBegin(EntityWorld world, Entity entity, AnimationGraph graph);

    void NotifyEnd(EntityWorld world, Entity entity, AnimationGraph graph);
}
