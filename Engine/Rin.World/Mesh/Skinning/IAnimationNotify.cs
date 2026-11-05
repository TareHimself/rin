using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Mesh.Skinning;

public interface IAnimationNotify : IPooledNotify
{
    /// <summary>
    ///     Called after the graph advanced, outside any query loop, so structural changes are allowed.
    /// </summary>
    void Notify(World world, Entity entity, AnimationGraph graph);
}
