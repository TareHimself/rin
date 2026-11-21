using System.Numerics;
using Rin.World.Graphics;
using Rin.World.Scheduling;
using Rin.World.Transforms;

namespace Rin.World.Animation;

/// <summary>
///     Runs after <see cref="AnimationSystem" />. Copies each socketed child's bone matrix from its parent's pose into
///     <see cref="AttachSocket.SocketMatrix" /> only when it changed, so propagation picks up the socket's change tick.
/// </summary>
internal sealed class BoneSocketSync : FrameSystem
{
    private readonly World _world;
    private readonly PoseSources _poseSources;
    private readonly Query<AttachSocket> _sockets;

    public BoneSocketSync(World world, PoseSources poseSources)
    {
        _world = world;
        _poseSources = poseSources;
        _sockets = world.Query<AttachSocket>();
    }

    public override void Update(in FrameContext context)
    {
        foreach (var chunk in _sockets)
            for (var i = 0; i < chunk.Length; i++)
            {
                ref readonly var socket = ref chunk.Components1[i];
                if (!TryGetBoneMatrices(chunk.Entities[i], out var bones) || (uint)socket.BoneIndex >= (uint)bones.Length) continue;

                ref readonly var bone = ref bones[socket.BoneIndex];
                if (bone == socket.SocketMatrix) continue;

                chunk.Write<AttachSocket>(i).SocketMatrix = bone;
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
