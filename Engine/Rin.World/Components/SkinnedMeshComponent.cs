using System.Numerics;
using Rin.Core.Extensions;
using Rin.Core.Shared.Math;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Mesh;
using Rin.World.Math;
using Rin.World.Mesh.Skinning;
using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Components;

public class SkinnedMeshComponent : WorldComponent
{
    public IMeshMaterial?[] Materials = [];
    public SkinnedMesh? Mesh { get; set; }
    public IPoseSource? PoseSource { get; set; }

    private RenderProxyHandle _proxy = RenderProxyHandle.Invalid;
    private uint _lastPushedVersion;
    private Matrix4x4[]? _resolvedBoneMatrices;

    public override void Start()
    {
        base.Start();
        if (Mesh is not null && IMeshFactory.Get().GetMesh(Mesh.MeshId) is { } mesh)
        {
            var (surfaceIndices, materials) = ResolveSurfaceMaterials(mesh);
            _proxy = Owner!.World!.RenderSystem.CreateSkinnedMeshProxy(new SkinnedMeshProxyDesc
            {
                Skeleton = Mesh.Skeleton,
                Pose = PoseSource?.GetPose() ?? Mesh.Skeleton.BasePose,
                Mesh = mesh,
                Transform = GetTransform(Space.World).ToMatrix(),
                SurfaceIndices = surfaceIndices,
                Materials = materials
            });
            _lastPushedVersion = TransformVersion;
        }
    }

    public override void Stop()
    {
        if (_proxy.IsValid)
        {
            Owner!.World!.RenderSystem.DestroyProxy(_proxy);
            _proxy = RenderProxyHandle.Invalid;
        }

        base.Stop();
    }

    public override void Update(float deltaSeconds)
    {
        base.Update(deltaSeconds);
        PoseSource?.Tick(deltaSeconds);

        if (PoseSource is AnimationGraph graph)
        {
            foreach (var notify in graph.FiredNotifies) notify.Notify(this, graph);
            foreach (var state in graph.BegunNotifyStates) state.NotifyBegin(this, graph);
            foreach (var state in graph.EndedNotifyStates) state.NotifyEnd(this, graph);
        }

        ResolveBoneMatrices();
    }

    private void ResolveBoneMatrices()
    {
        if (Mesh is null)
        {
            _resolvedBoneMatrices = null;
            return;
        }

        _resolvedBoneMatrices = Mesh.Skeleton.ResolvePose(PoseSource?.GetPose() ?? Mesh.Skeleton.BasePose);
        MarkWorldTransformDirty();
    }

    public override Transform GetAttachPointTransform(string? name)
    {
        if (name is not null && Mesh is not null && _resolvedBoneMatrices is not null &&
            Mesh.Skeleton.BoneNameToIndex.TryGetValue(name, out var boneIndex))
            return Transform.From(_resolvedBoneMatrices[boneIndex] * GetTransform(Space.World).ToMatrix());

        return base.GetAttachPointTransform(name);
    }

    public override void LateUpdate(float deltaSeconds)
    {
        base.LateUpdate(deltaSeconds);
        if (!_proxy.IsValid) return;
        var worldTransform = GetTransform(Space.World);
        if (TransformVersion != _lastPushedVersion)
        {
            Owner!.World!.RenderSystem.UpdateProxyTransform(_proxy, worldTransform.ToMatrix());
            _lastPushedVersion = TransformVersion;
        }

        if (PoseSource is { } poseSource) Owner!.World!.RenderSystem.UpdateSkinnedProxyPose(_proxy, poseSource.GetPose());
    }

    protected override void CollectSelf(CommandList commandList, Matrix4x4 transform)
    {
        if (Mesh is not null && IMeshFactory.Get().GetMesh(Mesh.MeshId) is { } mesh)
        {
            var (surfaceIndices, materials) = ResolveSurfaceMaterials(mesh);
            commandList.AddSkinned(new SkinnedMeshInfo
            {
                Skeleton = Mesh.Skeleton,
                Pose = PoseSource?.GetPose() ?? Mesh.Skeleton.BasePose,
                Mesh = mesh,
                Transform = transform,
                SurfaceIndices = surfaceIndices,
                Materials = materials
            });
        }
    }

    private (int[] SurfaceIndices, IMeshMaterial[] Materials) ResolveSurfaceMaterials(IMesh mesh)
    {
        var surfaces = mesh.GetSurfaces();
        IMeshMaterial lastMaterial = DefaultMeshMaterial.DefaultMesh;
        List<IMeshMaterial> materials = [];
        for (var i = 0; i < surfaces.Length; i++)
        {
            var material = lastMaterial = Materials.TryGet(i) ?? lastMaterial;
            materials.Add(material);
        }

        return (Enumerable.Range(0, surfaces.Length).ToArray(), materials.ToArray());
    }
}
