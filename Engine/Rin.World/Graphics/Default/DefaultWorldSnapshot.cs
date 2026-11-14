using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared.Math;
using Rin.Core.Shared.Pooling;

namespace Rin.World.Graphics.Default;

/// <summary>
///     View-independent, read-only copy of a world's render proxies, shared by every view of that world until the
///     proxies change. It owns its data: holders call <see cref="Acquire" /> and <see cref="Release" />, and the
///     last release hands it back to its render system to fill again.
/// </summary>
public sealed class DefaultWorldSnapshot : IResettable
{
    private readonly List<Transform> _bones = [];
    private readonly List<LightInfo> _lights = [];
    private readonly DefaultRenderSystem _owner;
    private readonly List<SkinnedMeshInfo> _skinned = [];
    private readonly List<StaticMeshInfo> _static = [];
    private int _holders;

    internal DefaultWorldSnapshot(DefaultRenderSystem owner)
    {
        _owner = owner;
    }

    public ReadOnlySpan<StaticMeshInfo> StaticGeometry => CollectionsMarshal.AsSpan(Held(_static));

    public ReadOnlySpan<SkinnedMeshInfo> SkinnedGeometry => CollectionsMarshal.AsSpan(Held(_skinned));

    public ReadOnlySpan<LightInfo> Lights => CollectionsMarshal.AsSpan(Held(_lights));

    /// <summary>
    ///     The mesh's local bone transforms, one per bone of its skeleton.
    /// </summary>
    public ReadOnlySpan<Transform> BonesOf(in SkinnedMeshInfo mesh)
    {
        return CollectionsMarshal.AsSpan(Held(_bones)).Slice(mesh.BoneStart, mesh.BoneCount);
    }

    public DefaultSceneFrame Write(IGraphBuilder builder)
    {
        return builder.GetOrAddShared(this, b => new DefaultSceneFrame(this, b));
    }

    public void Acquire()
    {
        Interlocked.Increment(ref _holders);
    }

    public void Release()
    {
        if (Interlocked.Decrement(ref _holders) == 0) _owner.Recycle(this);
    }

    internal void AddStaticMesh(in StaticMeshProxyDesc desc, in Matrix4x4 transform)
    {
        _static.Add(new StaticMeshInfo
        {
            Mesh = desc.Mesh,
            Transform = transform,
            SurfaceIndices = desc.SurfaceIndices,
            Materials = desc.Materials
        });
    }

    /// <summary>
    ///     Copies the mesh's pose in, so later changes to the pose it came from do not reach readers.
    /// </summary>
    internal void AddSkinnedMesh(in SkinnedMeshProxyDesc desc, in Matrix4x4 transform)
    {
        var boneStart = _bones.Count;
        var boneCount = desc.Skeleton.Bones.Length;
        CollectionsMarshal.SetCount(_bones, boneStart + boneCount);
        desc.Skeleton.CompleteLocalPose(desc.Pose, CollectionsMarshal.AsSpan(_bones).Slice(boneStart, boneCount));

        _skinned.Add(new SkinnedMeshInfo
        {
            Skeleton = desc.Skeleton,
            Mesh = desc.Mesh,
            Transform = transform,
            SurfaceIndices = desc.SurfaceIndices,
            Materials = desc.Materials,
            BoneStart = boneStart,
            BoneCount = boneCount
        });
    }

    internal void AddLight(in LightInfo light)
    {
        _lights.Add(light);
    }

    public void Reset()
    {
        if (Volatile.Read(ref _holders) > 0)
            throw new InvalidOperationException("A snapshot is reset only once nothing holds it.");
        _static.Clear();
        _skinned.Clear();
        _lights.Clear();
        _bones.Clear();
    }

    private List<T> Held<T>(List<T> list)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _holders) <= 0, this);
        return list;
    }
}
