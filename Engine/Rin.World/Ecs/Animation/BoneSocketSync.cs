using System.Numerics;
using Rin.World.Ecs.Graphics;
using Rin.World.Ecs.Scheduling;
using Rin.World.Ecs.Transforms;

namespace Rin.World.Ecs.Animation;

/// <summary>
///     Runs after <see cref="AnimationSystem" />. Copies each socketed child's bone matrix from its parent's pose into
///     <see cref="AttachSocket.SocketMatrix" /> and marks the child dirty when it changed, so propagation picks it up.
/// </summary>
internal sealed class BoneSocketSync : FrameSystem
{
    private readonly EntityWorld _world;
    private readonly PoseSources _poseSources;
    private readonly Query<AttachSocket, LocalTransform> _sockets;

    public BoneSocketSync(EntityWorld world, PoseSources poseSources)
    {
        _world = world;
        _poseSources = poseSources;
        _sockets = world.Query<AttachSocket, LocalTransform>();
    }

    public override void Update(in FrameContext context)
    {
        foreach (var (sockets, locals, entities) in _sockets)
            for (var i = 0; i < sockets.Length; i++)
            {
                ref var socket = ref sockets[i];
                if (!TryGetBoneMatrices(entities[i], out var bones) || (uint)socket.BoneIndex >= (uint)bones.Length) continue;

                ref readonly var bone = ref bones[socket.BoneIndex];
                if (bone == socket.SocketMatrix) continue;

                socket.SocketMatrix = bone;
                locals[i].IsDirty = true;
            }
    }

    private bool TryGetBoneMatrices(Entity child, out ReadOnlySpan<Matrix4x4> bones)
    {
        var parent = _world.GetParent(child);
        if (!parent.IsValid || !_world.Has<SkinnedMesh>(parent))
        {
            bones = default;
            return false;
        }

        bones = _poseSources.Entry(_world.Get<SkinnedMesh>(parent).PoseSource).BoneMatrices;
        return true;
    }
}
