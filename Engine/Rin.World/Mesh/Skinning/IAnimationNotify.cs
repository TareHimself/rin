using Rin.World.Components;
using Rin.World.Ecs;
using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Mesh.Skinning;

public interface IAnimationNotify : IPooledNotify
{
    void Notify(SkinnedMeshComponent meshComponent, AnimationGraph graph)
    {
    }

    /// <summary>
    ///     Called after the graph advanced, outside any query loop, so structural changes are allowed.
    /// </summary>
    void Notify(EntityWorld world, Entity entity, AnimationGraph graph);
}
