using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Mesh.Skinning;

public interface IAnimationNotifyState : IPooledNotify
{
    /// <summary>
    ///     Called after the graph advanced, outside any query loop, so structural changes are allowed.
    /// </summary>
    void NotifyBegin(World world, Entity entity, AnimationGraph graph);

    void NotifyEnd(World world, Entity entity, AnimationGraph graph);
}
