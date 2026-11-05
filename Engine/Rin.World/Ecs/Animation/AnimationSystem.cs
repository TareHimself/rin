using Rin.World.Ecs.Graphics;
using Rin.World.Ecs.Scheduling;
using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Ecs.Animation;

/// <summary>
///     Advances the pose source of every <see cref="SkinnedMesh" /> by the scaled frame delta, then dispatches the
///     notifies an <see cref="AnimationGraph" /> fired.
/// </summary>
/// <remarks>
///     A pose source shared by several entities advances once per frame and notifies with the first of them.
/// </remarks>
internal sealed class AnimationSystem : FrameSystem
{
    private readonly EntityWorld _world;
    private readonly PoseSources _poseSources;
    private readonly Query<SkinnedMesh> _meshes;
    private readonly List<(PoseSourceEntry Entry, Entity Entity)> _claimed = [];
    private long _frame;

    public AnimationSystem(EntityWorld world, PoseSources poseSources)
    {
        _world = world;
        _poseSources = poseSources;
        _meshes = world.Query<SkinnedMesh>();
    }

    public override void Update(in FrameContext context)
    {
        _frame++;
        ClaimPoseSources();

        // Advanced after the query loop so notifies may spawn, despawn and add components directly.
        for (var i = 0; i < _claimed.Count; i++)
        {
            var (entry, entity) = _claimed[i];
            entry.Advance(context.DeltaSeconds);
            if (entry.Source is AnimationGraph graph) DispatchNotifies(entity, graph);
        }

        _claimed.Clear();
    }

    private void ClaimPoseSources()
    {
        foreach (var (meshes, entities) in _meshes)
            for (var i = 0; i < meshes.Length; i++)
            {
                var entry = _poseSources.Entry(meshes[i].PoseSource);
                if (entry.TryClaimFrame(_frame)) _claimed.Add((entry, entities[i]));
            }
    }

    private void DispatchNotifies(Entity entity, AnimationGraph graph)
    {
        var fired = graph.FiredNotifies;
        for (var i = 0; i < fired.Count; i++) fired[i].Notify(_world, entity, graph);

        var begun = graph.BegunNotifyStates;
        for (var i = 0; i < begun.Count; i++) begun[i].NotifyBegin(_world, entity, graph);

        var ended = graph.EndedNotifyStates;
        for (var i = 0; i < ended.Count; i++) ended[i].NotifyEnd(_world, entity, graph);

        graph.ReleaseNotifies();
    }
}
