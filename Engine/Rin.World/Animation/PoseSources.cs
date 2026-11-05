using System.Numerics;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Animation;

/// <summary>
///     Keeps managed pose sources, such as an <see cref="Mesh.Skinning.Animation.AnimationGraph" />, out of components.
/// </summary>
public sealed class PoseSources
{
    private readonly List<PoseSourceEntry?> _entries = [null];

    public PoseSourceHandle Add(IPoseSource source)
    {
        _entries.Add(new PoseSourceEntry(source));
        return new PoseSourceHandle(_entries.Count - 1);
    }

    public IPoseSource Get(PoseSourceHandle handle) => Entry(handle).Source;

    internal PoseSourceEntry Entry(PoseSourceHandle handle) =>
        handle.IsValid && handle.Id < _entries.Count
            ? _entries[handle.Id]!
            : throw new ArgumentException($"{handle} is not a pose source of this registry.", nameof(handle));
}

/// <summary>
///     A pose source and its model-space bone matrices, resolved only when a socket asks after the pose changed.
/// </summary>
internal sealed class PoseSourceEntry(IPoseSource source)
{
    private readonly Matrix4x4[] _boneMatrices = new Matrix4x4[source.Skeleton.Bones.Length];
    private bool _boneMatricesStale = true;
    private long _advancedFrame = -1;

    public IPoseSource Source { get; } = source;

    public ReadOnlySpan<Matrix4x4> BoneMatrices
    {
        get
        {
            if (_boneMatricesStale)
            {
                Source.Skeleton.ResolvePose(Source.GetPose(), _boneMatrices);
                _boneMatricesStale = false;
            }

            return _boneMatrices;
        }
    }

    /// <summary>
    ///     False when the source already advanced this frame, so a source shared by several entities advances once.
    /// </summary>
    public bool TryClaimFrame(long frame)
    {
        if (_advancedFrame == frame) return false;
        _advancedFrame = frame;
        return true;
    }

    public void Advance(float deltaSeconds)
    {
        Source.Tick(deltaSeconds);
        _boneMatricesStale = true;
    }
}
