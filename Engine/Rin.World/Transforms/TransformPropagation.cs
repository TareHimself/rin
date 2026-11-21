using System.Numerics;
using Rin.World.Ecs;
using Rin.World.Scheduling;

namespace Rin.World.Transforms;

/// <summary>
///     Built-in first <see cref="FramePhase.Extract" /> system. Walks every hierarchy depth first from its roots and
///     recomputes <see cref="WorldTransform" /> for changed entities and for children whose parent matrix changed.
/// </summary>
/// <remarks>
///     An entity is changed when its <see cref="LocalTransform" />, <see cref="AttachSocket" /> or
///     <see cref="WorldTransform" /> was written since the last run. A subtree is reached only through parents that have
///     both transform components. A child with <see cref="AttachSocket" /> is placed relative to its socket instead of
///     its parent's origin.
/// </remarks>
internal sealed class TransformPropagation : FrameSystem
{
    private readonly World _world;
    private readonly Query<LocalTransform, WorldTransform> _transforms;
    private readonly ComponentId[] _changeSources;
    private ChangeWindow _window;
    private PendingNode[] _pending = new PendingNode[64];
    private int _pendingCount;

    public TransformPropagation(World world)
    {
        _world = world;
        _transforms = world.Query<LocalTransform, WorldTransform>();
        _changeSources = [world.Schema.IdOf<LocalTransform>(), world.Schema.IdOf<WorldTransform>(), world.Schema.IdOf<AttachSocket>()];
        world.OnRemove<AttachSocket>(MarkLocalChanged);
    }

    public override void Update(in FrameContext context)
    {
        _window.BeginRun(_world.ChangeTick);
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

        var worldMatrix = _world.Get<WorldTransform>(entity).Matrix;
        var changed = false;
        if (node.ParentChanged || _world.HasAnyChangedSince(entity, _changeSources, _window.Since))
        {
            var matrix = _world.ComputeLocalMatrix(entity, _world.Get<LocalTransform>(entity)) * node.ParentWorld;
            if (matrix != worldMatrix)
            {
                _world.Write<WorldTransform>(entity).Matrix = matrix;
                worldMatrix = matrix;
                changed = true;
            }
        }

        var children = _world.GetChildren(entity);
        for (var i = children.Count - 1; i >= 0; i--) Push(new PendingNode(children[i], worldMatrix, changed));
    }

    // Removing the socket changes no transform row, so stamp one to have the entity placed relative to its parent again.
    private void MarkLocalChanged(Entity entity, in AttachSocket socket)
    {
        if (_world.Has<LocalTransform>(entity)) _world.Write<LocalTransform>(entity);
    }

    private void Push(in PendingNode node)
    {
        if (_pendingCount == _pending.Length) Array.Resize(ref _pending, _pending.Length * 2);
        _pending[_pendingCount++] = node;
    }

    private readonly record struct PendingNode(Entity Entity, Matrix4x4 ParentWorld, bool ParentChanged);
}
