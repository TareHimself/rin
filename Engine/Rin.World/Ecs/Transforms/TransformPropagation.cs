using System.Numerics;
using Rin.World.Ecs.Scheduling;

namespace Rin.World.Ecs.Transforms;

/// <summary>
///     Built-in <see cref="FramePhase.Late" /> system. Walks every hierarchy depth first from its roots and recomputes
///     <see cref="WorldTransform" /> for dirty entities and for children whose parent matrix changed.
/// </summary>
/// <remarks>
///     A subtree is reached only through parents that have both transform components.
/// </remarks>
internal sealed class TransformPropagation : FrameSystem
{
    private readonly EntityWorld _world;
    private readonly Query<LocalTransform, WorldTransform> _transforms;
    private PendingNode[] _pending = new PendingNode[64];
    private int _pendingCount;

    public TransformPropagation(EntityWorld world)
    {
        _world = world;
        _transforms = world.Query<LocalTransform, WorldTransform>();
    }

    public override void Update(in FrameContext context)
    {
        foreach (var chunk in _transforms)
            for (var i = 0; i < chunk.Length; i++)
            {
                var entity = chunk.Entities[i];
                if (!_world.GetParent(entity).IsValid) Push(new PendingNode(entity, Matrix4x4.Identity, false));
            }

        while (_pendingCount > 0) Visit(_pending[--_pendingCount]);
    }

    private void Visit(in PendingNode node)
    {
        var entity = node.Entity;
        if (!_world.Has<LocalTransform>(entity) || !_world.Has<WorldTransform>(entity)) return;

        ref var local = ref _world.Get<LocalTransform>(entity);
        var dirty = local.IsDirty;
        local.IsDirty = false;

        ref var worldTransform = ref _world.Get<WorldTransform>(entity);
        var changed = false;
        if (dirty || node.ParentChanged)
        {
            var matrix = local.ToMatrix() * node.ParentWorld;
            if (matrix != worldTransform.Matrix)
            {
                worldTransform.Matrix = matrix;
                worldTransform.Version++;
                changed = true;
            }
        }

        var children = _world.GetChildren(entity);
        for (var i = children.Count - 1; i >= 0; i--) Push(new PendingNode(children[i], worldTransform.Matrix, changed));
    }

    private void Push(in PendingNode node)
    {
        if (_pendingCount == _pending.Length) Array.Resize(ref _pending, _pending.Length * 2);
        _pending[_pendingCount++] = node;
    }

    private readonly record struct PendingNode(Entity Entity, Matrix4x4 ParentWorld, bool ParentChanged);
}
